using System.Xml.Linq;

namespace JackAnalyzer;

internal class CompilationEngine
{
    private List<string> _DeclaredClasses = new List<string>();

    public CompilationEngine(string[] filepaths)
    {
        if (filepaths.Length == 0)
            throw new ArgumentException("At least one file path to translate should be provided.");

        ForwardDeclareClasses(filepaths);
        
        Process(filepaths);
    }

    private void ForwardDeclareClasses(string[] filepaths)
    {
        foreach (var filepath in filepaths)
        {
            var tokenXml = new Tokenizer(filepath).Process();

            try
            {
                _DeclaredClasses.Add(CompileClass(tokenXml, identifyOnly: true));
            }
            catch (Exception ex)
            {
                throw new Exception($"Compiler forward declare error for {filepath}: {ex}");
            }
        }
    }

    private void Process(string[] filepaths)
    {
        foreach (var filepath in filepaths)
        {
            var tokenXml = new Tokenizer(filepath).Process();

            try
            {
                var compiledXml = CompileClass(tokenXml);
            }
            catch (Exception ex)
            {
                throw new Exception($"Compiler error for {filepath}: {ex}");
            }
        }
    }

    private string CompileClass(string tokenXml, bool identifyOnly = false)
    {
        var tokenElement = XElement.Parse(tokenXml);
        var xmlLines = new List<string> { "<class>" };

        var elements = tokenElement.Elements().ToList();
        if (elements.Count > 3)
        {
            if (elements[0].Name == "keyword" && elements[0].Value == "class" &&
                elements[1].Name == "identifier" &&
                elements[2].Name == "symbol" && elements[2].Value == "{")
            {
                xmlLines.Add(elements[0].ToString());
                xmlLines.Add(elements[1].ToString());
                xmlLines.Add(elements[2].ToString());

                if (identifyOnly)
                    return elements[1].Value;

                xmlLines.AddRange(HandleClassDeclarations(elements.Skip(3)));
            }
        }
        else
        {
            throw new Exception("Invalid class declaration.");
        }

        xmlLines.Add("</class>");

        return string.Concat(xmlLines);
    }

    private IEnumerable<string> HandleClassDeclarations(IEnumerable<XElement> elements)
    {
        var xmlLines = new List<string>();

        foreach (var element in elements)
        {
            if (element.Name == "keyword" && new[] { "static", "field" }.Contains(element.Value))
            {
                xmlLines.AddRange(CompileClassVarDec(element));
            }
        }

        return xmlLines;
    }

    private IEnumerable<string> CompileClassVarDec(XElement element)
    {
        var xmlLines = new List<string> { "<classVarDec>" };

        if (element.NextNode is XElement typeElement)
        {
            xmlLines.Add(element.ToString());
            
            if ((typeElement.Name == "keyword" && new[] { "int", "char", "boolean" }.Contains(typeElement.Value)) ||
                (typeElement.Name == "identifier" && _DeclaredClasses.Contains(typeElement.Value)))
            {
                xmlLines.Add(typeElement.ToString());
              
                if (typeElement.NextNode is XElement varName)
                {
                    xmlLines.AddRange(HandleIdentifiers(varName));
                }
                else
                {
                    throw new Exception("Nothing to process after variable keyword.");
                }
            }
            else
            {
                throw new Exception("Invalid keyword defined for class variable.");
            }
        }
        else
        {
            throw new Exception("Nothing to process after class defined variable.");
        }

        xmlLines.Add("</classVarDec>");

        return xmlLines;
    }

    private IEnumerable<string> HandleIdentifiers(XElement element)
    {
        var xmlLines = new List<string>();

        if (element.Name == "identifier")
        {
            xmlLines.Add(element.ToString());

            if (element.NextNode is XElement symbol)
            {
                if (symbol.Name == "symbol" && symbol.Value == ",")
                {
                    xmlLines.Add(symbol.ToString());

                    if (symbol.NextNode is XElement value)
                    {
                        xmlLines.AddRange(HandleIdentifiers(value));
                    }
                }
                else if (symbol.Name == "symbol" && symbol.Value == ";")
                {
                    xmlLines.Add(symbol.ToString());
                }
                else
                {
                    throw new Exception("Invalid symbol specified for identifier");
                }
            }
            else
            {
                throw new Exception("Nothing to process after identifier");
            }
        }
        else
        {
            throw new Exception("Identifier expected.");
        }

        return xmlLines;
    }
}
