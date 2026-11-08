// Alan Medeiros
using AcademiaDoZe.Application.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AcademiaDoZe.Presentation.AppMaui.ViewModels;

public class RestricaoSelectable : ObservableObject
{
    public AppMatriculaRestricoes Value { get; set; }

    public string Rotulo => Value.GetDisplayName();

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}
