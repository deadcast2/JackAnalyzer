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

    public XElement Advance()
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

    public XElement Reverse()
    {
        if (_NextElement.PreviousNode is XElement previous)
        {
            return _NextElement = previous;
        }

        return _NextElement;
    }

    public bool HasMoreTokens()
    {
        return _NextElement.NextNode != null;
    }
}
