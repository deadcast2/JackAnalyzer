using System.Xml.Linq;

namespace JackAnalyzer;

internal class TokenIterator
{
    private readonly XElement _RootElement;
    private XElement _NextElement;

    public TokenIterator(string xml)
    {
        _RootElement = _NextElement = XElement.Parse(xml);
    }

    public XElement Next()
    {
        if (_NextElement == _RootElement)
        {
            return _NextElement = (_RootElement.FirstNode as XElement) ?? new XElement("null");
        }

        if (_NextElement.NextNode is XElement next)
        {
            return _NextElement = next;
        }

        return _NextElement;
    }

    public XElement Peek() => _NextElement.NextNode as XElement ?? new XElement("null");

    public XElement Current() => _NextElement;

    public string CurrentAsString() => Current().ToString();

    public bool HasMore()
    {
        return _NextElement.NextNode != null || _RootElement.FirstNode != null;
    }
}
