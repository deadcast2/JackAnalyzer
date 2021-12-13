namespace JackAnalyzer;

internal class CompilationEngine
{
    public CompilationEngine(string[] filePaths)
    {
        foreach (string path in filePaths)
        {
            CompileClass(new TokenIterator(new Tokenizer(path).Process()));
        }
    }

    private IEnumerable<string> CompileClass(TokenIterator it)
    {
        var xml = new List<string> { "<class>" };

        if (!it.HasMore())
            throw new Exception("Class definition missing");

        if (!it.Next().Is("keyword", "class"))
            throw new Exception("'Class' keyword expected.");

        xml.Add(it.CurrentAsString());

        if (!it.HasMore())
            throw new Exception("Class name expected.");

        if (!it.Next().Is("identifier"))
            throw new Exception("Class name identifier expected.");

        xml.Add(it.CurrentAsString());

        if (!it.HasMore())
            throw new Exception("Opening bracket for class expected.");

        if (!it.Next().Is("symbol", "{"))
            throw new Exception("'{' symbol expected for class.");

        xml.Add(it.CurrentAsString());

        xml.AddRange(CompileClassVarDec(it));

        xml.AddRange(CompileSubroutine(it));

        if (!it.HasMore())
            throw new Exception("Closing bracket for class expected.");

        if (!it.Next().Is("symbol", "}"))
            throw new Exception("'}' symbol expected for class.");

        xml.Add(it.CurrentAsString());

        xml.Add("</class>");

        return xml;
    }

    private IEnumerable<string> CompileClassVarDec(TokenIterator it)
    {
        return CompileVarDec(it, "classVarDec", "field", "static");
    }

    private IEnumerable<string> CompileVarDec(TokenIterator it,
        string tagName, params string[] allowedValues)
    {
        var xml = new List<string>();

        if (!it.HasMore())
            return xml;

        if (!it.Peek().Is("keyword", allowedValues))
            return xml;

        xml.Add($"<{tagName}>");

        xml.Add(it.Next().ToString());

        if (!it.HasMore())
            throw new Exception($"Type expected for '{it.Current()}'.");

        if (!it.Next().Is("keyword", "int", "char", "boolean") &&
            !it.Current().Is("identifier", v => true /* validate class name */))
            throw new Exception($"Invalid type defined for field/static: '{it.Current()}'.");

        xml.Add(it.CurrentAsString());

        xml.AddRange(WriteVarName(it));

        xml.Add($"</{tagName}>");

        xml.AddRange(CompileVarDec(it, tagName, allowedValues));

        return xml;
    }

    private IEnumerable<string> CompileSubroutine(TokenIterator it)
    {
        var xml = new List<string>();

        if (!it.Next().Is("keyword", "constructor", "function", "method"))
            return xml;

        xml.Add("<subroutineDec>");
        xml.Add(it.CurrentAsString());

        if (!it.Next().Is("keyword", "void", "int", "char", "boolean") &&
            !it.Current().Is("identifier", v => true /* validate class name */))
            throw new Exception($"Invalid type defined for subroutine: '{it.Current()}'.");

        xml.Add(it.CurrentAsString());

        if (!it.Next().Is("identifier"))
            throw new Exception("Expected name for subroutine.");

        xml.Add(it.CurrentAsString());

        if (!it.Next().Is("symbol", "("))
            throw new Exception("Expected opening paranthesis for subroutine.");

        xml.Add(it.CurrentAsString());

        xml.AddRange(CompileParameterList(it));

        if (!it.Next().Is("symbol", ")"))
            throw new Exception("Expected closing paranthesis for subroutine.");

        xml.Add(it.CurrentAsString());

        xml.AddRange(CompileSubroutineBody(it));

        xml.Add("</subroutineDec>");

        xml.AddRange(CompileSubroutine(it));

        return xml;
    }

    private IEnumerable<string> CompileParameterList(TokenIterator it)
    {
        var xml = new List<string>();

        if (!it.HasMore())
            return xml;

        xml.Add("<parameterList>");

        if (it.Peek().Is("keyword"))
            xml.AddRange(WriteParamName(it));

        xml.Add("</parameterList>");

        return xml;
    }

    private IEnumerable<string> WriteParamName(TokenIterator it)
    {
        var xml = new List<string>();

        if (!it.Next().Is("keyword", "int", "char", "boolean") &&
           !it.Current().Is("identifier", v => true /* validate class name */))
            throw new Exception($"Invalid type defined for parameter list: '{it.Current()}'.");

        xml.Add(it.CurrentAsString());

        if (!it.HasMore())
            throw new Exception("Identifier for parameter expected.");

        if (!it.Next().Is("identifier"))
            throw new Exception("Invalid indentifier for parameter.");

        xml.Add(it.CurrentAsString());

        if (it.Peek().Is("symbol", ","))
        {
            xml.Add(it.Next().ToString());
            xml.AddRange(WriteParamName(it));
        }

        return xml;
    }

    private IEnumerable<string> WriteVarName(TokenIterator it)
    {
        var xml = new List<string>();

        if (!it.HasMore())
            throw new Exception("Var name expected.");

        if (!it.Next().Is("identifier"))
            throw new Exception("Identifier expected for var name.");

        xml.Add(it.CurrentAsString());

        if (it.Next().Is("symbol", ","))
        {
            xml.Add(it.CurrentAsString());
            xml.AddRange(WriteVarName(it));
        }
        else if (it.Current().Is("symbol", ";"))
        {
            xml.Add(it.CurrentAsString());
        }
        else
        {
            throw new Exception("Line ending expected for field/static.");
        }

        return xml;
    }

    private IEnumerable<string> CompileSubroutineBody(TokenIterator it)
    {
        var xml = new List<string> { "<subroutineBody>" };

        if (!it.HasMore())
            throw new Exception("Expected subroutine body.");

        if (!it.Next().Is("symbol", "{"))
            throw new Exception("'{' missing for subroutine body.");

        xml.Add(it.CurrentAsString());

        xml.AddRange(CompileVarDec(it, "varDec", "var"));

        xml.AddRange(CompileStatements(it));

        xml.Add("</subroutineBody>");

        return xml;
    }

    private IEnumerable<string> CompileStatements(TokenIterator it)
    {
        var xml = new List<string> { "<statements>" };

        xml.AddRange(CompileLet(it));

        xml.Add("</statements>");

        return xml;
    }

    private IEnumerable<string> CompileLet(TokenIterator it)
    {
        var xml = new List<string> { "<letStatement>" };

        if (!it.HasMore())
            return xml;

        if (!it.Next().Is("keyword", "let"))
            return xml;

        xml.Add(it.CurrentAsString());

        if (!it.HasMore())
            throw new Exception("Identifier expected for let statement.");

        if (!it.Next().Is("identifier"))
            throw new Exception("Defined indentifier expected for let statement.");

        xml.Add(it.CurrentAsString());

        if (!it.HasMore())
            throw new Exception("Equals expected for let statement.");

        if (!it.Next().Is("symbol"))
            throw new Exception("Defined equals expected for let statement.");

        xml.Add(it.CurrentAsString());

        xml.AddRange(CompileExpression(it));

        if (!it.HasMore())
            throw new Exception("Ending expected for let statement.");

        if (!it.Next().Is("symbol"))
            throw new Exception("Defined ending expected for let statement.");

        xml.Add(it.CurrentAsString());

        xml.Add("</letStatement>");

        xml.AddRange(CompileLet(it));

        return xml;
    }

    private IEnumerable<string> CompileExpression(TokenIterator it)
    {
        var xml = new List<string> { "<expression>" };

        xml.AddRange(CompileTerm(it));

        xml.Add("</expression>");

        return xml;
    }

    private IEnumerable<string> CompileTerm(TokenIterator it)
    {
        var xml = new List<string> { "<term>" };

        if (!it.HasMore())
            throw new Exception("Term expected for expression.");

        if (!it.Next().Is("identifier"))
            throw new Exception("Defined identifier expected for term expression.");

        xml.Add(it.CurrentAsString());

        xml.Add("</term>");

        return xml;
    }
}
