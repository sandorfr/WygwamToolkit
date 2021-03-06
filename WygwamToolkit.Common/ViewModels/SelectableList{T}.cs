﻿//-----------------------------------------------------------------------
// <copyright file="SelectableList{T}.cs" company="Wygwam">
//     Copyright (c) 2013 Wygwam.
//     Licensed under the Microsoft Public License (Ms-PL) (the "License");
//     you may not use this file except in compliance with the License.
//     You may obtain a copy of the License at
//
//         http://opensource.org/licenses/Ms-PL.html
//
//     Unless required by applicable law or agreed to in writing, software
//     distributed under the License is distributed on an "AS IS" BASIS,
//     WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//     See the License for the specific language governing permissions and
//     limitations under the License.
// </copyright>
//-----------------------------------------------------------------------
namespace Wygwam.Windows.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Linq;

    /// <summary>
    /// An <see cref="System.Collections.ObjectModel.ObservableCollection{T}" /> that tracks
    /// instances of <see cref="SelectableItem{T}" /> and raises the <see cref="E:SelectedItemsChanged" />
    /// event when the selection changes.
    /// </summary>
    /// <typeparam name="T">The type of the items stored in each <see cref="SelectableItem{T}" />.</typeparam>
    public class SelectableList<T> : ObservableCollection<SelectableItem<T>>
    {
        private List<int> _selectedItemsIndexes;
        private ObservableCollection<T> _selectedItems;
        private bool _maintainsSelectedItemsOrder;

        /// <summary>
        /// Initializes a new instance of the <see cref="SelectableList{T}" /> class.
        /// </summary>
        public SelectableList()
            : base()
        {
            _selectedItems = new ObservableCollection<T>();
            _selectedItemsIndexes = new List<int>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SelectableList{T}" /> class that contains elements
        /// copied from the specified collection.
        /// </summary>
        /// <param name="collection">The collection from which elements are copied.</param>
        public SelectableList(IEnumerable<SelectableItem<T>> collection)
            : base(collection)
        {
            _selectedItems = new ObservableCollection<T>();
            _selectedItemsIndexes = new List<int>();

            foreach (var item in collection)
            {
                this.BindToSelectableItem(item);
            }
        }

        /// <summary>
        /// Occurs when an item from the collection is selected or deselected.
        /// </summary>
        public event EventHandler<SelectableItemChangedEventArgs<T>> SelectedItemsChanged;

        /// <summary>
        /// Gets a list of selected items.
        /// </summary>
        public ObservableCollection<T> SelectedItems
        {
            get
            {
                return _selectedItems;
            }
        }

        /// <summary>
        /// Gets or sets a value that determines whether the list of selected items maintains the order
        /// of the items in the original list.
        /// </summary>
        public bool MaintainsSelectedItemsOrder
        {
            get { return _maintainsSelectedItemsOrder; }

            set
            {
                if (_maintainsSelectedItemsOrder != value)
                {
                    _maintainsSelectedItemsOrder = value;
                    this.OnPropertyChanged(new PropertyChangedEventArgs("MaintainsSelectedItemsOrder"));
                }
            }
        }

        /// <summary>
        /// Called when the <see cref="E:CollectionChanged" /> event is raised by the base class,
        /// this is where the collection subscribes to the PropertyChanged event of its items.
        /// </summary>
        /// <param name="e">The <see cref="System.Collections.Specialized.NotifyCollectionChangedEventArgs"/> instance containing the event data.</param>
        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnCollectionChanged(e);

            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var item in e.NewItems.Cast<SelectableItem<T>>())
                {
                    this.BindToSelectableItem(item);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove ||
                     e.Action == NotifyCollectionChangedAction.Replace ||
                     e.Action == NotifyCollectionChangedAction.Reset)
            {
                foreach (var item in e.OldItems.Cast<SelectableItem<T>>())
                {
                    item.PropertyChanged -= this.OnItemPropertyChanged;
                }
            }
        }

        /// <summary>
        /// Raises the <see cref="E:SelectedItemsChanged"/> event.
        /// </summary>
        /// <param name="e">The <see cref="SelectableItemChangedEventArgs{T}"/> instance containing the event data.</param>
        protected void OnSelectedItemsChanged(SelectableItemChangedEventArgs<T> e)
        {
            var handler = this.SelectedItemsChanged;

            this.UpdateSelectedItemsList(e.ChangedItem);

            if (handler != null)
            {
                handler(this, e);
            }
        }

        private void BindToSelectableItem(SelectableItem<T> item)
        {
            item.PropertyChanged += this.OnItemPropertyChanged;

            this.UpdateSelectedItemsList(item);
        }

        /// <summary>
        /// Updates the selected items list by adding or removing the <see cref="SelectableItem{T}"/>.
        /// </summary>
        /// <param name="item">A <see cref="SelectableItem{T}"/> that will be added or removed from the selected
        /// items list.</param>
        private void UpdateSelectedItemsList(SelectableItem<T> item)
        {
            var idx = this.IndexOf(item);

            if (!item.IsSelected)
            {
                if (this.MaintainsSelectedItemsOrder)
                {
                    _selectedItemsIndexes.Remove(idx);
                }

                _selectedItems.Remove(item.Item);
            }
            else
            {
                if (this.MaintainsSelectedItemsOrder)
                {
                    if (_selectedItemsIndexes.Count == 0 || _selectedItemsIndexes[_selectedItemsIndexes.Count - 1] < idx)
                    {
                        _selectedItemsIndexes.Add(idx);
                        _selectedItems.Add(item.Item);
                    }
                    else if (_selectedItemsIndexes[0] > idx)
                    {
                        _selectedItemsIndexes.Insert(0, idx);
                        _selectedItems.Insert(0, item.Item);
                    }
                    else
                    {
                        int indexOfNextGreatest = _selectedItemsIndexes.IndexOf(_selectedItemsIndexes.Where(i => i > idx).First());

                        _selectedItemsIndexes.Insert(indexOfNextGreatest, idx);
                        _selectedItems.Insert(indexOfNextGreatest, item.Item);
                    }
                }
                else
                {
                    _selectedItems.Add(item.Item);
                }
            }
        }

        /// <summary>
        /// Called when a property of an item changed. Raises the <see cref="E:SelectedItemsChanged"/> if
        /// the property is <c>IsSelected</c>.
        /// </summary>
        /// <param name="sender">The item whose property changed.</param>
        /// <param name="e">The <see cref="PropertyChangedEventArgs"/> instance containing the event data.</param>
        private void OnItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("IsSelected", StringComparison.Ordinal))
            {
                this.OnSelectedItemsChanged(new SelectableItemChangedEventArgs<T>(sender as SelectableItem<T>));
            }
        }
    }
}
