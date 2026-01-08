using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace SEToolbox.Services
{
    // http://social.msdn.microsoft.com/Forums/vstudio/en-US/10713000-8069-4277-bab2-249f2f0af131/mvvm-question-syncing-a-collection-between-the-model-and-the-viewmodel?forum=wpf

    /// <summary>
    /// A collection of ViewModel objects that wraps a collection of Model objects,
    /// with each ViewModel object wrapping its' corresponding Model object.
    /// The two collections are synchronized.
    /// </summary>
    /// <typeparam name="TViewModel"></typeparam>
    /// <typeparam name="TModel"></typeparam>
    public class ViewModelCollection<TViewModel, TModel> : IList<TViewModel>, INotifyCollectionChanged
       where TViewModel : IModelWrapper
    {
        #region Private Members

        /// <summary>
        /// Contains all VM objects
        /// </summary>
        readonly List<TViewModel> _list;

        /// <summary>
        /// Reference to the collection in the model, which is wrapped by this collection
        /// </summary>
        readonly ObservableCollection<TModel> _model;

        /// <summary>
        /// A method to create a VM object from a Model object (possibly VM constructor)
        /// </summary>
        readonly Func<TModel, TViewModel> _createViewModel;

        #endregion //Private Members

        /// <summary>
        /// Creates a collection of VM objects
        ///
        /// Note that the IModelWrapper constraint on TViewModel, combined with the
        /// ViewModelFactory member, we get bidirectionality: we can both create a VM
        /// from model, and also get the model from a VM.
        /// </summary>
        /// <param name="modelCollection">Reference to the collection in the model</param>
        /// <param name="viewModelCreator">A method to create a VM object from a Model object (possibly VM constructor)</param>
        public ViewModelCollection(ObservableCollection<TModel> modelCollection, Func<TModel, TViewModel> viewModelCreator)
        {
            _model = modelCollection ?? throw new ArgumentNullException(nameof(modelCollection));
            _model.CollectionChanged += new(Model_CollectionChanged);

            _createViewModel = viewModelCreator ?? throw new ArgumentNullException(nameof(viewModelCreator));

            //inits the list to reflect initial state of model collection
            _list = [.. from m in _model select viewModelCreator(m)];
        }

        /// <summary>
        /// Listens to changes in the model collection and changes the VM list accordingly.
        /// This is the only place where changes to the list of VM objects (_list) occur, to
        /// guarantee that it represents the model collection.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Model_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var getModel = new Func<object, TModel>(vm => (TModel)((IModelWrapper)vm).GetModel());
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    int newIndex = e.NewStartingIndex;
                    var viewModelsToAdd = e.NewItems.Cast<TModel>().Select(_createViewModel);
                    InsertNewIndex(newIndex, viewModelsToAdd);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    var oldIndex = e.OldStartingIndex;
                    var itemsToRemove = e.OldItems.Cast<object>().Select(getModel).ToList();
                    RemoveOldIndex(oldIndex, itemsToRemove);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    Reset(e.NewItems?.Cast<object>().Select(getModel) ?? []);
                    break;
                default:
                    break;
            } 
   }
        private void InsertNewIndex(int newIndex, IEnumerable<TViewModel> viewModelsToAdd)
        {
            foreach (var viewModel in viewModelsToAdd)
            {
                if (newIndex < _list.Count)
                {
                    _list.Insert(newIndex, viewModel);
                }
                else
                {
                    _list.Add(viewModel);
                }
                OnCollectionChanged(NotifyCollectionChangedAction.Add, viewModel, newIndex);
            }
        }

        private void RemoveOldIndex(int oldIndex, IEnumerable<TModel> itemsToRemove)
        {
            foreach (var item in itemsToRemove)
            {
                int index = _list.FindIndex(vm => vm.GetModel() == (object)item);
                if (index != -1)
                {
                    _list.RemoveAt(index);
                    OnCollectionChanged(NotifyCollectionChangedAction.Remove, _list[index], oldIndex);
                }
            }
        }

        private void Reset(IEnumerable<TModel> items)
        {
            _list.Clear();
            foreach (var item in items)
            {
                _list.Add(_createViewModel(item));
            }
           
        }
     

        
        #region IList<T> Implementation

        public int IndexOf(TViewModel item)
        {
            return _list.IndexOf(item);
        }

        public void Insert(int index, TViewModel item)
        {
            _model.Insert(index, (TModel)item.GetModel());
        }

        public void RemoveAt(int index)
        {
            Remove(_list[index]);
        }

        public TViewModel this[int index]
        {
            get => _list[index];
            set => _list[index] = value;
        }

        public void Add(TViewModel item)
        {
            //note that _list is not changed directly
            _model.Add((TModel)item.GetModel());
        }

        public void Clear()
        {
            _model.Clear();
        }

        public bool Contains(TViewModel item)
        {
            return _list.Contains(item);
        }

        public void CopyTo(TViewModel[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get => _list.Count;
        }

        public bool IsReadOnly
        {
            get => false;
        }

        public bool Remove(TViewModel item)
        {
            //note that _list is not changed directly
            return _model.Remove((TModel)item.GetModel());
        }

        public IEnumerator<TViewModel> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return (_list as System.Collections.IEnumerable).GetEnumerator();
        }

        #endregion //IList<T> Implementation

        #region INotifyCollectionChanged Implementation

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        protected void OnCollectionChanged(NotifyCollectionChangedAction action, object item, int index)
        {
            var handler = CollectionChanged;
            if (handler != null)
            {
                /* Note that there exist several ctors for NotifyCollectionChangedEventArgs
                 * and it may be required to call one of the other, more complex ctors
                 * for the change to take effect on all UI elements */
                var e = new NotifyCollectionChangedEventArgs(action, item, index);
                handler?.Invoke(this, e);
            }
        }

        #endregion //INotifyCollectionChanged Implementation
    }

    public interface IModelWrapper
    {
        object GetModel();
    }
}
