using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace WpfSample
{
    public class MainViewModel : INotifyPropertyChanged, IDisposable
    {
        private string _namespace;
        private readonly Subject<string> _namespaceChanged = new();
        private readonly CompositeDisposable _namespaceChangedSubscription = new();

        public MainViewModel()
        {
            _namespaceChangedSubscription.Add(_namespaceChanged
                .ObserveOn(Scheduler.CurrentThread)
                .Subscribe(_ =>
                {
                    IsLoading = true;
                    OnPropertyChanged(nameof(IsLoading));
                }));

            _namespaceChangedSubscription.Add(_namespaceChanged
                .Throttle(TimeSpan.FromMilliseconds(1000))
                .SelectMany(ParseNamespace)
                .ObserveOn(Scheduler.CurrentThread)
                .Subscribe(parsedNamespace =>
                {
                    OnPropertyChanged(nameof(OperationsCount));
                    ParsedNamespace = parsedNamespace;
                    OnPropertyChanged(nameof(ParsedNamespace));
                    IsLoading = false;
                    OnPropertyChanged(nameof(IsLoading));
                }));
        }

        async Task<IReadOnlyList<string>> ParseNamespace(string newNamespace)
        {
            await Task.Delay(1000);
            OperationsCount++;
            return newNamespace.Split('.');
        }

        public string Namespace
        {
            get => _namespace;
            set
            {
                _namespace = value;
                _namespaceChanged.OnNext(value);
            }
        }

        public IReadOnlyList<string> ParsedNamespace { get; set; }
        public bool IsLoading { get; set; }

        public int OperationsCount { get; set; }

        public void Dispose()
        {
            _namespaceChangedSubscription.Dispose();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}