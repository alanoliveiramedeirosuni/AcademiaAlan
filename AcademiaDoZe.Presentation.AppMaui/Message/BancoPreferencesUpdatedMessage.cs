// Alan Medeiros
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace AcademiaDoZe.Presentation.AppMaui.Message;

// ValueChangedMessage<T> é uma classe base do toolkit para mensagens que carregam um valor.
public sealed class BancoPreferencesUpdatedMessage(string value) : ValueChangedMessage<string>(value)
{
}
