namespace JackAnalyzer;

internal class CompilationEngine
{
    private List<string> _DeclaredClasses = new List<string>();

    public CompilationEngine(string[] filePaths)
    {
        foreach (var filePath in filePaths)
        {
            ForwardDeclareClasses(filePath);
        }

        foreach (var filePath in filePaths)
        {
            Compile(filePath);
        }
    }

    private void ForwardDeclareClasses(string filePath)
    {
        try
        {
            _DeclaredClasses.AddRange(CompileClass(filePath, identifyOnly: true));
        }
        catch (Exception ex)
        {
            throw new Exception($"Compiler forward declare error: {ex}");
        }
    }

    private void Compile(string filePath)
    {
        try
        {
            var compiledXml = CompileClass(filePath);

            File.WriteAllLines(Path.ChangeExtension(filePath, "xml"), compiledXml);
        }
        catch (Exception ex)
        {
            throw new Exception($"Compiler error: {ex}");
        }
    }

    private IEnumerable<string> CompileClass(string filePath, bool identifyOnly = false)
    {
        var xmlLines = new List<string> { "<class>" };
        var tokenIterator = new TokenIterator(new Tokenizer(filePath).Process());

        if (tokenIterator.HasMoreTokens())
            throw new Exception("No code to process.");

        var currentToken = tokenIterator.Advance();

        if (currentToken.Name == "keyword" && currentToken.Value == "class")
        {
            if (!tokenIterator.HasMoreTokens())
                throw new Exception("No class name specified.");

            xmlLines.Add(currentToken.ToString());

            currentToken = tokenIterator.Advance();

            if (currentToken.Name == "identifier")
            {
                var identifier = currentToken.Value;

                if (!tokenIterator.HasMoreTokens())
                    throw new Exception("Class missing opening curly brace.");

                xmlLines.Add(currentToken.ToString());

                currentToken = tokenIterator.Advance();

                if (currentToken.Name == "symbol" && currentToken.Value == "{")
                {
                    if (identifyOnly) return new[] { identifier };

                    xmlLines.Add(currentToken.ToString());
                    xmlLines.AddRange(HandleClassDeclarations(tokenIterator));

                    if (tokenIterator.HasMoreTokens())
                    {
                        currentToken = tokenIterator.Advance();

                        if (currentToken.Name == "symbol" && currentToken.Value == "}")
                        {
                            xmlLines.Add(currentToken.ToString());
                        }
                    }
                }
            }
        }

        xmlLines.Add("</class>");

        return xmlLines;
    }

    private IEnumerable<string> HandleClassDeclarations(TokenIterator tokenIterator)
    {
        var xmlLines = new List<string>();

        while (tokenIterator.HasMoreTokens())
        {
            var currentToken = tokenIterator.Advance();

            if (currentToken.Name == "keyword")
            {
                if (new[] { "static", "field" }.Contains(currentToken.Value))
                {
                    xmlLines.AddRange(CompileClassVarDec(currentToken.ToString(), tokenIterator));
                }
                else if (new[] { "constructor", "function", "method" }.Contains(currentToken.Value))
                {
                    xmlLines.AddRange(CompileSubroutineDec(currentToken.ToString(), tokenIterator));
                }
                else
                {
                    throw new InvalidOperationException("Invalid keyword defined.");
                }
            }
        }

        tokenIterator.Reverse();

        return xmlLines;
    }

    private IEnumerable<string> CompileClassVarDec(string parentToken, TokenIterator tokenIterator)
    {
        var xmlLines = new List<string> { "<classVarDec>", parentToken };

        if (tokenIterator.HasMoreTokens())
        {
            var currentToken = tokenIterator.Advance();

            if ((currentToken.Name == "keyword" && new[] { "int", "char", "boolean" }.Contains(currentToken.Value)) ||
                (currentToken.Name == "identifier" && _DeclaredClasses.Contains(currentToken.Value)))
            {
                xmlLines.Add(currentToken.ToString());
                xmlLines.AddRange(HandleIdentifiers(tokenIterator));
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

    private IEnumerable<string> CompileSubroutineDec(string parentToken, TokenIterator tokenIterator)
    {
        var xmlLines = new List<string> { "<subroutineDec>", parentToken };

        if (tokenIterator.HasMoreTokens())
        {
            var currentToken = tokenIterator.Advance();

            if ((currentToken.Name == "keyword" && new[] { "int", "char", "boolean", "void" }.Contains(currentToken.Value)) ||
                (currentToken.Name == "identifier" && _DeclaredClasses.Contains(currentToken.Value)))
            {
                xmlLines.Add(currentToken.ToString());

                if (tokenIterator.HasMoreTokens())
                {
                    currentToken = tokenIterator.Advance();

                    if (currentToken.Name == "identifier")
                    {
                        xmlLines.Add(currentToken.ToString());
                        xmlLines.AddRange(CompileParameterList(tokenIterator));
                        xmlLines.AddRange(CompileSubroutineBody(tokenIterator));
                    }
                }
            }
        }

        xmlLines.Add("</subroutineDec>");

        return xmlLines;
    }

    private IEnumerable<string> CompileSubroutineBody(TokenIterator tokenIterator)
    {
        var xmlLines = new List<string>();

        xmlLines.Add("<subroutineBody>");

        if (tokenIterator.HasMoreTokens())
        {
            var currentToken = tokenIterator.Advance();

            if (currentToken.Name == "symbol" && currentToken.Value == "{")
            {
                xmlLines.Add(currentToken.ToString());
                xmlLines.AddRange(CompileStatements(tokenIterator));

                if (tokenIterator.HasMoreTokens())
                {
                    currentToken = tokenIterator.Advance();

                    if (currentToken.Name == "symbol" && currentToken.Value == "}")
                    {
                        xmlLines.Add(currentToken.ToString());
                    }
                }
            }
        }

        xmlLines.Add("</subroutineBody>");

        return xmlLines;
    }

    private IEnumerable<string> CompileStatements(TokenIterator tokenIterator, bool skipListTag = false)
    {
        var xmlLines = new List<string>();

        if (!skipListTag) xmlLines.Add("<statements>");

        while (tokenIterator.HasMoreTokens())
        {
            var currentToken = tokenIterator.Advance();

            if (currentToken.Name == "keyword" && new[] { "let", "if", "while", "do", "return" }.Contains(currentToken.Value))
            {
                switch (currentToken.Value)
                {
                    case "let":
                        xmlLines.AddRange(CompileLet(tokenIterator));
                        break;
                    case "do":
                        xmlLines.AddRange(CompileDo(tokenIterator));
                        break;
                    case "return":
                        xmlLines.AddRange(CompileReturn(tokenIterator));
                        break;
                    case "if":
                        xmlLines.AddRange(CompileIf(tokenIterator));
                        break;
                }
            }
            else
            {
                break;
            }
        }

        if (!skipListTag)
        {
            xmlLines.Add("</statements>");

            // Don't lose the last token for additional processing after this.
            tokenIterator.Reverse();
        }

        return xmlLines;
    }

    private IEnumerable<string> CompileIf(TokenIterator tokenIterator)
    {
        var xmlLines = new List<string> { "<ifStatement>" };

        tokenIterator.Reverse();

        // Add the "if" keyword.
        xmlLines.Add(tokenIterator.Advance().ToString());

        if (tokenIterator.HasMoreTokens())
        {
            var currentToken = tokenIterator.Advance();

            if (currentToken.Name == "symbol" && currentToken.Value == "(")
            {
                xmlLines.Add(currentToken.ToString());
                xmlLines.AddRange(CompileExpression(tokenIterator));

                if (tokenIterator.HasMoreTokens())
                {
                    currentToken = tokenIterator.Advance();

                    if (currentToken.Name == "symbol" && currentToken.Value == ")")
                    {
                        xmlLines.Add(currentToken.ToString());

                        if (tokenIterator.HasMoreTokens())
                        {
                            currentToken = tokenIterator.Advance();

                            if (currentToken.Name == "symbol" && currentToken.Value == "{")
                            {
                                xmlLines.Add(currentToken.ToString());
                                xmlLines.AddRange(CompileStatements(tokenIterator));

                                if (tokenIterator.HasMoreTokens())
                                {
                                    currentToken = tokenIterator.Advance();

                                    if (currentToken.Name == "symbol" && currentToken.Value == "}")
                                    {
                                        xmlLines.Add(currentToken.ToString());
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        xmlLines.Add("</ifStatement>");

        return xmlLines;
    }

    private IEnumerable<string> CompileReturn(TokenIterator tokenIterator)
    {
        var xmlLines = new List<string> { "<returnStatement>" };

        tokenIterator.Reverse();

        // Add the "return" keyword.
        xmlLines.Add(tokenIterator.Advance().ToString());
        xmlLines.AddRange(CompileExpression(tokenIterator));

        if (tokenIterator.HasMoreTokens())
        {
            var currentToken = tokenIterator.Advance();

            if (currentToken.Name == "symbol" && currentToken.Value == ";")
            {
                xmlLines.Add(currentToken.ToString());
            }
        }

        xmlLines.Add("</returnStatement>");

        return xmlLines;
    }

    private IEnumerable<string> CompileDo(TokenIterator tokenIterator)
    {
        var xmlLines = new List<string> { "<doStatement>" };

        tokenIterator.Reverse();

        // Add the "do" keyword.
        xmlLines.Add(tokenIterator.Advance().ToString());

        if (tokenIterator.HasMoreTokens())
        {
            var currentToken = tokenIterator.Advance();

            if (currentToken.Name == "identifier")
            {
                xmlLines.Add(currentToken.ToString());

                if (tokenIterator.HasMoreTokens())
                {
                    currentToken = tokenIterator.Advance();

                    if (currentToken.Name == "symbol" && currentToken.Value == ".")
                    {
                        xmlLines.Add(currentToken.ToString());

                        if (tokenIterator.HasMoreTokens())
                        {
                            currentToken = tokenIterator.Advance();

                            if (currentToken.Name == "identifier")
                            {
                                xmlLines.Add(currentToken.ToString());

                                // Advance to get ready for expression list.
                                currentToken = tokenIterator.Advance();
                            }
                        }
                    }

                    if (currentToken.Name == "symbol" && currentToken.Value == "(")
                    {
                        xmlLines.Add(currentToken.ToString());
                        xmlLines.AddRange(CompileExpressionList(tokenIterator));

                        if (tokenIterator.HasMoreTokens())
                        {
                            currentToken = tokenIterator.Advance();

                            if (currentToken.Name == "symbol" && currentToken.Value == ")")
                            {
                                xmlLines.Add(currentToken.ToString());
                            }
                        }
                    }

                    if (tokenIterator.HasMoreTokens())
                    {
                        currentToken = tokenIterator.Advance();

                        if (currentToken.Name == "symbol" && currentToken.Value == ";")
                        {
                            xmlLines.Add(currentToken.ToString());
                        }
                    }
                }
            }
        }

        xmlLines.Add("</doStatement>");

        return xmlLines;
    }

    private IEnumerable<string> CompileExpressionList(TokenIterator tokenIterator)
    {
        var xmlLines = new List<string> { "<expressionList>" };

        while (tokenIterator.HasMoreTokens())
        {
            xmlLines.AddRange(CompileExpression(tokenIterator));

            if (tokenIterator.HasMoreTokens())
            {
                var currentToken = tokenIterator.Advance();

                if (currentToken.Name == "symbol" && currentToken.Value == ",")
                {
                    xmlLines.Add(currentToken.ToString());
                    continue;
                }
                else
                {
                    tokenIterator.Reverse();
                    break;
                }
            }
        }

        xmlLines.Add("</expressionList>");

        return xmlLines;
    }

    private IEnumerable<string> CompileLet(TokenIterator tokenIterator)
    {
        var xmlLines = new List<string> { "<letStatement>" };

        tokenIterator.Reverse();

        // Add the "let" keyword.
        xmlLines.Add(tokenIterator.Advance().ToString());

        if (tokenIterator.HasMoreTokens())
        {
            var currentToken = tokenIterator.Advance();

            if (currentToken.Name == "identifier")
            {
                xmlLines.Add(currentToken.ToString());

                if (tokenIterator.HasMoreTokens())
                {
                    currentToken = tokenIterator.Advance();

                    if (currentToken.Name == "symbol" && currentToken.Value == "[")
                    {
                        // handle array
                    }
                    else if (currentToken.Name == "symbol" && currentToken.Value == "=")
                    {
                        xmlLines.Add(currentToken.ToString());
                        xmlLines.AddRange(CompileExpression(tokenIterator));
                    }

                    if (tokenIterator.HasMoreTokens())
                    {
                        currentToken = tokenIterator.Advance();

                        if (currentToken.Name == "symbol" && currentToken.Value == ";")
                        {
                            xmlLines.Add(currentToken.ToString());
                        }
                    }
                }
            }
        }

        xmlLines.Add("</letStatement>");

        return xmlLines;
    }

    private IEnumerable<string> CompileExpression(TokenIterator tokenIterator)
    {
        var xmlLines = new List<string> { "<expression>" };

        if (tokenIterator.HasMoreTokens())
        {
            var currentToken = tokenIterator.Advance();

            if (new[] { "identifier", "keyword" }.Contains(currentToken.Name.ToString()))
            {
                xmlLines.Add("<term>");
                xmlLines.Add(currentToken.ToString());
                xmlLines.Add("</term>");
            }
            else
            {
                tokenIterator.Reverse();

                return new List<string>();
            }
        }

        xmlLines.Add("</expression>");

        return xmlLines;
    }

    private IEnumerable<string> CompileParameterList(TokenIterator tokenIterator)
    {
        var xmlLines = new List<string>();

        if (tokenIterator.HasMoreTokens())
        {
            var currentToken = tokenIterator.Advance();

            if (currentToken.Name == "symbol" && currentToken.Value == "(")
            {
                xmlLines.Add(currentToken.ToString());

                if (tokenIterator.HasMoreTokens())
                {
                    xmlLines.AddRange(HandleParameters(tokenIterator));

                    if (tokenIterator.HasMoreTokens())
                    {
                        currentToken = tokenIterator.Advance();

                        if (currentToken.Name == "symbol" && currentToken.Value == ")")
                        {
                            xmlLines.Add(currentToken.ToString());
                        }
                    }
                }
            }
        }

        return xmlLines;
    }

    private IEnumerable<string> HandleParameters(TokenIterator tokenIterator, bool skipListTag = false)
    {
        var xmlLines = new List<string>();

        if (!skipListTag) xmlLines.Add("<parameterList>");

        if (tokenIterator.HasMoreTokens())
        {
            var currentToken = tokenIterator.Advance();

            if ((currentToken.Name == "keyword" && new[] { "int", "char", "boolean" }.Contains(currentToken.Value)) ||
                (currentToken.Name == "identifier" && _DeclaredClasses.Contains(currentToken.Value)))
            {
                xmlLines.Add(currentToken.ToString());

                if (tokenIterator.HasMoreTokens())
                {
                    currentToken = tokenIterator.Advance();

                    if (currentToken.Name == "identifier")
                    {
                        xmlLines.Add(currentToken.ToString());

                        if (tokenIterator.HasMoreTokens())
                        {
                            currentToken = tokenIterator.Advance();

                            if (currentToken.Name == "symbol" && currentToken.Value == ",")
                            {
                                xmlLines.Add(currentToken.ToString());
                                xmlLines.AddRange(HandleParameters(tokenIterator, skipListTag: true));
                            }
                        }
                    }
                }
            }
        }

        if (!skipListTag)
        {
            xmlLines.Add("</parameterList>");

            // Don't lose the last token for additional processing after this.
            tokenIterator.Reverse();
        }

        return xmlLines;
    }

    private IEnumerable<string> HandleIdentifiers(TokenIterator tokenIterator)
    {
        var xmlLines = new List<string>();

        if (tokenIterator.HasMoreTokens())
        {
            var currentToken = tokenIterator.Advance();

            if (currentToken.Name == "identifier")
            {
                xmlLines.Add(currentToken.ToString());

                if (tokenIterator.HasMoreTokens())
                {
                    currentToken = tokenIterator.Advance();

                    if (currentToken.Name == "symbol" && currentToken.Value == ",")
                    {
                        xmlLines.Add(currentToken.ToString());
                        xmlLines.AddRange(HandleIdentifiers(tokenIterator));
                    }
                    else if (currentToken.Name == "symbol" && currentToken.Value == ";")
                    {
                        xmlLines.Add(currentToken.ToString());
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
        }

        return xmlLines;
    }
}
