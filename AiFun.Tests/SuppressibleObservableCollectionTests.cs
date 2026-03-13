using System.Collections.Specialized;
using System.ComponentModel;
using AiFun;
using AiFun.Entities;

namespace AiFun.Tests;

public class SuppressibleObservableCollectionTests
{
    [Fact]
    public void Add_WhenNotSuppressed_RaisesCollectionChanged()
    {
        var collection = new SuppressibleObservableCollection<string>();
        var raised = false;
        collection.CollectionChanged += (s, e) => raised = true;

        collection.Add("item");

        Assert.True(raised);
    }

    [Fact]
    public void Add_WhenSuppressed_DoesNotRaiseCollectionChanged()
    {
        var collection = new SuppressibleObservableCollection<string>();
        var raised = false;
        collection.CollectionChanged += (s, e) => raised = true;

        AiFun.Entities.Object.SuppressNotifications = true;
        try
        {
            collection.Add("item");
        }
        finally
        {
            AiFun.Entities.Object.SuppressNotifications = false;
        }

        Assert.False(raised);
    }

    [Fact]
    public void Add_WhenSuppressed_StillModifiesUnderlyingCollection()
    {
        var collection = new SuppressibleObservableCollection<string>();

        AiFun.Entities.Object.SuppressNotifications = true;
        try
        {
            collection.Add("item");
        }
        finally
        {
            AiFun.Entities.Object.SuppressNotifications = false;
        }

        Assert.Single(collection);
        Assert.Equal("item", collection[0]);
    }

    [Fact]
    public void Remove_WhenSuppressed_DoesNotRaiseCollectionChanged()
    {
        var collection = new SuppressibleObservableCollection<string>();
        collection.Add("item");

        var raised = false;
        collection.CollectionChanged += (s, e) => raised = true;

        AiFun.Entities.Object.SuppressNotifications = true;
        try
        {
            collection.Remove("item");
        }
        finally
        {
            AiFun.Entities.Object.SuppressNotifications = false;
        }

        Assert.False(raised);
    }

    [Fact]
    public void FlushSuppressedChanges_AfterSuppressedAdd_RaisesResetNotification()
    {
        var collection = new SuppressibleObservableCollection<string>();
        NotifyCollectionChangedAction? action = null;
        collection.CollectionChanged += (s, e) => action = e.Action;

        AiFun.Entities.Object.SuppressNotifications = true;
        collection.Add("item");
        AiFun.Entities.Object.SuppressNotifications = false;

        // Reset action tracking after suppression ends
        action = null;
        collection.FlushSuppressedChanges();

        Assert.Equal(NotifyCollectionChangedAction.Reset, action);
    }

    [Fact]
    public void FlushSuppressedChanges_WhenNoSuppressedChanges_DoesNotRaiseEvent()
    {
        var collection = new SuppressibleObservableCollection<string>();
        collection.Add("item"); // normal add

        var raised = false;
        collection.CollectionChanged += (s, e) => raised = true;

        collection.FlushSuppressedChanges();

        Assert.False(raised);
    }

    [Fact]
    public void PropertyChanged_WhenSuppressed_DoesNotRaise()
    {
        var collection = new SuppressibleObservableCollection<string>();
        var raisedProperties = new List<string>();
        ((INotifyPropertyChanged)collection).PropertyChanged += (s, e) =>
            raisedProperties.Add(e.PropertyName!);

        AiFun.Entities.Object.SuppressNotifications = true;
        try
        {
            collection.Add("item");
        }
        finally
        {
            AiFun.Entities.Object.SuppressNotifications = false;
        }

        Assert.Empty(raisedProperties);
    }

    [Fact]
    public void FlushSuppressedChanges_RaisesCountAndItemPropertyChanged()
    {
        var collection = new SuppressibleObservableCollection<string>();

        AiFun.Entities.Object.SuppressNotifications = true;
        collection.Add("item");
        AiFun.Entities.Object.SuppressNotifications = false;

        var raisedProperties = new List<string>();
        ((INotifyPropertyChanged)collection).PropertyChanged += (s, e) =>
            raisedProperties.Add(e.PropertyName!);

        collection.FlushSuppressedChanges();

        Assert.Contains("Count", raisedProperties);
        Assert.Contains("Item[]", raisedProperties);
    }
}
