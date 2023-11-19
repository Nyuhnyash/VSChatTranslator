using System.Collections.Generic;


namespace ChatTranslator
{
    public interface ITranslator
    {
        IEnumerable<string> SupportedLanguages { get; }
        string Translate(string message, string from, string to);
    }
}
