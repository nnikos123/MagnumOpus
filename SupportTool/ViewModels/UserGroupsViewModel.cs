﻿using ReactiveUI;
using SupportTool.Helpers;
using SupportTool.Models;
using SupportTool.Services.ActiveDirectoryServices;
using SupportTool.Services.NavigationServices;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.DirectoryServices;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace SupportTool.ViewModels
{
    public class UserGroupsViewModel : ReactiveObject
    {
        private readonly Subject<Message> messages;

        private readonly ReactiveCommand<Unit, Unit> openAddGroups;
        private readonly ReactiveCommand<Unit, Unit> removeGroup;
        private readonly ReactiveCommand<Unit, IEnumerable<DirectoryEntry>> getAllGroups;
        private readonly ReactiveList<DirectoryEntry> allGroups;
        private readonly ReactiveList<string> directGroups;
        private readonly ListCollectionView allGroupsCollectionView;
        private readonly ListCollectionView directGroupsCollectionView;
        private readonly ObservableAsPropertyHelper<bool> isLoadingGroups;
        private UserObject user;
        private bool isShowingDirectGroups;
        private bool isShowingAllGroups;
        private object selectedDirectGroup;
        private string groupFitler;
        private bool useFuzzy;



        public UserGroupsViewModel()
        {
            messages = new Subject<Message>();
            allGroups = new ReactiveList<DirectoryEntry>();
            directGroups = new ReactiveList<string>();

            allGroupsCollectionView = new ListCollectionView(allGroups);
            allGroupsCollectionView.Filter = TextFilter;
            this
                .WhenAnyValue(x => x.GroupFilter, y => y.UseFuzzy)
                .Subscribe(_ => AllGroupsCollectionView?.Refresh());

            directGroupsCollectionView = new ListCollectionView(directGroups);
            directGroupsCollectionView.SortDescriptions.Add(new SortDescription());

            openAddGroups = ReactiveCommand.CreateFromTask(() => NavigationService.Current.NavigateTo<Views.AddGroupsWindow>(user.Principal.SamAccountName));

            removeGroup = ReactiveCommand.CreateFromObservable(() => Observable.Start(() =>
            {
                var group = ActiveDirectoryService.Current.GetGroup(selectedDirectGroup as string).Wait();
                group.Principal.Members.Remove(user.Principal);
                group.Principal.Save();
                MessageBus.Current.SendMessage(ApplicationActionRequest.LoadDirectGroupsForUser);
            }),
            this.WhenAnyValue(x => x.SelectedDirectGroup).Select(x => x != null));
            removeGroup
                .ThrownExceptions
                .Subscribe(ex => messages.OnNext(Message.Error(ex.Message, "Could not remove group")));

            getAllGroups = ReactiveCommand.CreateFromObservable(
                () => GetGroupsImpl(User.Principal.SamAccountName)
                        .TakeUntil(this.WhenAnyValue(x => x.IsShowingAllGroups).Where(x => !x)),
                this.WhenAnyValue(x => x.IsShowingAllGroups, y => y.AllGroups.Count, (x,y) => x && y == 0));
            getAllGroups
                .Subscribe(x =>
                {
                    using (AllGroups.SuppressChangeNotifications())
                    {
                        AllGroups.Clear();
                        AllGroups.AddRange(x.OrderBy(y => y.Path));
                    }
                });
            getAllGroups
                .ThrownExceptions
                .Subscribe(ex => messages.OnNext(Message.Error(ex.Message, "Couldn't get groups")));
            getAllGroups
                .IsExecuting
                .ToProperty(this, x => x.IsLoadingGroups, out isLoadingGroups);

            this
                .WhenAnyValue(x => x.User)
                .Subscribe(_ => ResetValues());

            this
                .WhenAnyValue(x => x.User)
                .Where(x => x != null)
                .Merge(MessageBus.Current.Listen<ApplicationActionRequest>().Where(x => x == ApplicationActionRequest.LoadDirectGroupsForUser).Select(_ => User))
                .Do(_ => DirectGroups.Clear())
                .SelectMany(x => GetDirectGroups(x))
                .Subscribe(x => DirectGroups.Add(x.Properties.Get<string>("cn")));

            this
                .WhenAnyValue(x => x.IsShowingDirectGroups)
                .Where(x => x)
                .Subscribe(_ => IsShowingAllGroups = false);

            this
                .WhenAnyValue(x => x.IsShowingAllGroups)
                .Where(x => x)
                .Subscribe(_ => IsShowingDirectGroups = false);
        }



        public IObservable<Message> Messages => messages;

        public ReactiveCommand OpenAddGroups => openAddGroups;

        public ReactiveCommand RemoveGroup => removeGroup;

        public ReactiveCommand GetAllGroups => getAllGroups;

        public ReactiveList<DirectoryEntry> AllGroups => allGroups;

        public ReactiveList<string> DirectGroups => directGroups;

        public ListCollectionView AllGroupsCollectionView => allGroupsCollectionView;

        public ListCollectionView DirectGroupsCollectionView => directGroupsCollectionView;

        public bool IsLoadingGroups => isLoadingGroups.Value;

        public UserObject User
        {
            get { return user; }
            set { this.RaiseAndSetIfChanged(ref user, value); }
        }

        public bool IsShowingDirectGroups
        {
            get { return isShowingDirectGroups; }
            set { this.RaiseAndSetIfChanged(ref isShowingDirectGroups, value); }
        }

        public bool IsShowingAllGroups
        {
            get { return isShowingAllGroups; }
            set { this.RaiseAndSetIfChanged(ref isShowingAllGroups, value); }
        }

        public object SelectedDirectGroup
        {
            get { return selectedDirectGroup; }
            set { this.RaiseAndSetIfChanged(ref selectedDirectGroup, value); }
        }

        public string GroupFilter
        {
            get { return groupFitler; }
            set { this.RaiseAndSetIfChanged(ref groupFitler, value); }
        }

        public bool UseFuzzy
        {
            get { return useFuzzy; }
            set { this.RaiseAndSetIfChanged(ref useFuzzy, value); }
        }



        private void ResetValues()
        {
            AllGroups.Clear();
            DirectGroups.Clear();
            IsShowingDirectGroups = false;
            IsShowingAllGroups = false;
            GroupFilter = "";
        }

        private IObservable<DirectoryEntry> GetDirectGroups(UserObject user) => Observable.Create<DirectoryEntry>(async observer =>
        {
            var disposed = false;

            foreach (string item in user.MemberOf)
            {
                var de = await ActiveDirectoryService.Current.GetGroups("group", "distinguishedname", item).Take(1);

                if (disposed) break;
                observer.OnNext(de);
            }

            observer.OnCompleted();

            return () => disposed = true;
        });

        private IObservable<IEnumerable<DirectoryEntry>> GetGroupsImpl(string samAccountName) => Observable.StartAsync(async () =>
        {
            var result = new List<DirectoryEntry>();

            foreach (var element in User.MemberOf)
            {
                var name = element.ToString();
                result.AddRange(await ActiveDirectoryService.Current.GetParents(name, $"{User.Principal.SamAccountName}/" + name, result));
            }

            return result;
        });

        bool TextFilter(object item)
        {
            if (!GroupFilter.HasValue()) { return true; }

            var itm = ((DirectoryEntry)item).Properties["cn"][0].ToString().ToLowerInvariant().Replace(" ", string.Empty);

            itm = itm.ToLowerInvariant();

            if (UseFuzzy)
            {
                var filterString = GroupFilter.Replace(" ", string.Empty).ToLowerInvariant();

                var idx = 0;

                foreach (var letter in itm)
                {
                    if (letter == filterString[idx])
                    {
                        idx += 1;
                        if (idx >= filterString.Length) { return true; }
                    }
                }
            }
            else
            {
                if (itm.Contains(GroupFilter.ToLowerInvariant()))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
