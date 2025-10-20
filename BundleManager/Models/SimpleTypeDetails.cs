using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace BundleManager.Models;

public partial class SimpleTypeDetails
{
	private readonly IList simpleObjectList;
	private readonly ObservableCollection<SimpleTypeDetails> detailsList;
	private readonly Type elementType;

	private int Index { get => detailsList.IndexOf(this); }

	public SimpleTypeDetails(IList simpleObjectList, ObservableCollection<SimpleTypeDetails> detailsList, Type elementType)
	{
		this.simpleObjectList = simpleObjectList;
		this.detailsList = detailsList;
		this.elementType = elementType;
	}

	public object? Value
	{
		get => simpleObjectList[Index];
		set
		{
			int index = Index;
			var converted = value == null ? null : Convert.ChangeType(value, elementType);
			if (!Equals(simpleObjectList[index], converted))
			{
				simpleObjectList[index] = converted;
				OnPropertyChanged(nameof(Value));
			}
		}
	}

	public event PropertyChangedEventHandler? PropertyChanged;
	protected void OnPropertyChanged(string propertyName) =>
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
