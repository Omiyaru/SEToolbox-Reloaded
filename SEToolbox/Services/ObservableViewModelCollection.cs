using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Collections.Generic;
namespace SEToolbox.Services
{
    // http://stackoverflow.com/questions/1256793/mvvm-sync-collections/2177659#2177659

    public class ObservableViewModelCollection<TViewModel, TModel> : ObservableCollection<TViewModel>
    {
        private readonly ObservableCollection<TModel> _source;
        private readonly Func<TModel, TViewModel> _viewModelFactory;

        public ObservableViewModelCollection(ObservableCollection<TModel> source, Func<TModel, TViewModel> viewModelFactory)
            : base(source == null ? [] : source.Select(viewModelFactory))
        {
            Contract.Requires(source != null);
            Contract.Requires(viewModelFactory != null);

            _source = source;
            _viewModelFactory = viewModelFactory;
            _source?.CollectionChanged += OnSourceCollectionChanged;
        }

        ~ObservableViewModelCollection()
        {
            _source?.CollectionChanged -= OnSourceCollectionChanged;
        }

        protected virtual TViewModel CreateViewModel(TModel model)
        {
            return _viewModelFactory(model);
        }

        private void OnSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            int newIndex = e.NewStartingIndex;
            int oldIndex = e.OldStartingIndex;
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    var viewModelsToAdd = e.NewItems.Cast<TModel>().Select(CreateViewModel);
                    InsertNewIndex(newIndex, viewModelsToAdd);
                    break;

                case NotifyCollectionChangedAction.Move:
                    var itemsToMove = this.Skip(oldIndex).Take(e.OldItems.Count).ToList();
                    RemoveOldIndex(oldIndex, e);
                    InsertNewIndex(newIndex, itemsToMove);
                    break;

                case NotifyCollectionChangedAction.Remove:
                     RemoveOldIndex(oldIndex, e);
                    break;
                case NotifyCollectionChangedAction.Replace:
                     RemoveOldIndex(oldIndex, e);
                     var viewModelsToReplace = e.NewItems.Cast<TModel>().Select(CreateViewModel);
                     InsertNewIndex(newIndex, viewModelsToReplace);
                    break;

                case NotifyCollectionChangedAction.Reset:
                    var viewModels = e.NewItems.Cast<TModel>().Select(CreateViewModel);
                     Reset(viewModels, e);
                    break;
                default:
                    break;
            }
        }

        private void RemoveOldIndex(int oldIndex, NotifyCollectionChangedEventArgs e)
        {
            for (int i = 0; i < e.OldItems.Count; i++)
                RemoveAt(oldIndex);
        }

        private void InsertNewIndex(int newIndex, IEnumerable<TViewModel> viewModels)
        {
            foreach (var viewModel in viewModels)
                Insert(newIndex++, viewModel);
        }

        private void AddViewModel(IEnumerable<TViewModel> viewModels)
        {
            foreach (var viewModel in viewModels)
                    Add(viewModel);
        }

        private void Reset(IEnumerable<TViewModel> viewModels, NotifyCollectionChangedEventArgs e)
        {
            Clear();
            if (e.NewItems.Count > 0)
                AddViewModel(viewModels);
        }
    }
}
