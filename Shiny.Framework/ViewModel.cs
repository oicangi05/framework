﻿using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Prism.AppModel;
using Prism.Navigation;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Shiny.Net;


namespace Shiny
{
    public abstract class ViewModel : ReactiveObject,
                                      IInitialize,
                                      IInitializeAsync,
                                      INavigatedAware,
                                      IPageLifecycleAware,
                                      IDestructible,
                                      IConfirmNavigationAsync
    {
        CompositeDisposable? deactivateWith;
        protected CompositeDisposable DeactivateWith => this.deactivateWith ??= new CompositeDisposable();
        protected CompositeDisposable DestroyWith { get; } = new CompositeDisposable();


        protected virtual void Deactivate()
        {
            this.deactivateWith?.Dispose();
            this.deactivateWith = null;
        }

        public virtual void OnNavigatedFrom(INavigationParameters parameters) => this.Deactivate();
        public virtual void Initialize(INavigationParameters parameters) { }
        public virtual Task InitializeAsync(INavigationParameters parameters) => Task.CompletedTask;
        public virtual void OnNavigatedTo(INavigationParameters parameters) { }
        public virtual void OnAppearing()
        {
            ShinyHost
                .Resolve<IConnectivity>()
                .WhenInternetStatusChanged()
                .ObserveOn(RxApp.MainThreadScheduler)
                .ToPropertyEx(this, x => x.IsInternetAvailable)
                .DisposeWith(this.DeactivateWith);
        }
        public virtual void OnDisappearing() { }
        public virtual void Destroy() => this.DestroyWith?.Dispose();
        public virtual Task<bool> CanNavigateAsync(INavigationParameters parameters) => Task.FromResult(true);

        [Reactive] public bool IsBusy { get; set; }
        [Reactive] public string? Title { get; protected set; }
        public bool IsInternetAvailable { [ObservableAsProperty] get; }


        IDialogs? dialogs;
        public IDialogs Dialogs
        {
            get
            {
                this.dialogs ??= ShinyHost.Resolve<IDialogs>();
                return this.dialogs;
            }
        }


        ILocalize? localize;
        public ILocalize Localize
        {
            get
            {
                this.localize ??= ShinyHost.Resolve<ILocalize>();
                return this.localize;
            }
        }

        public string this[string key] => this.Localize[key];


        protected void BindBusyCommand(ICommand command)
            => this.BindBusyCommand((IReactiveCommand)command);


        protected void BindBusyCommand(IReactiveCommand command) =>
            command.IsExecuting.Subscribe(
                x => this.IsBusy = x,
                _ => this.IsBusy = false,
                () => this.IsBusy = false
            )
            .DisposeWith(this.DeactivateWith);
    }
}
