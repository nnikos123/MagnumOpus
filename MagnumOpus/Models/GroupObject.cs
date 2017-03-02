﻿using MagnumOpus.Services.ActiveDirectoryServices;
using System;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Reactive.Linq;

namespace MagnumOpus.Models
{
	public class GroupObject : ActiveDirectoryObject<GroupPrincipal>
    {
        public GroupObject(GroupPrincipal principal) : base(principal) { }



        public string Description { get => _principal.Description.HasValue() ? _principal.Description : ""; set => _principal.Description = value.HasValue() ? value : null; }

		public string Notes => _directoryEntry.Properties.Get<string>("info");

        public IObservable<UserObject> GetManager() => Observable.Return(_directoryEntry.Properties.Get<string>("manager"))
            .SelectMany(x =>
            {
                if (x == null) return Observable.Return<UserObject>(null);
                else return ActiveDirectoryService.Current.GetUser(x);
            })
            .Catch(Observable.Return<UserObject>(null))
            .Take(1);
    }
}