using TeamNotificationService.Domain.Exceptions;
using TeamNotificationService.Application.Models.Common;
using TeamNotificationService.Application.Enums;

namespace TeamNotificationService.Api.Utils;

public static class FilterGroupConverter
{
    public static FilterGroup? Build(string? groupPattern, int criterionCount)
    {
        if (criterionCount <= 0)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(groupPattern))
        {
            return CreateAndGroup(CreateCriterionIndexes(criterionCount));
        }

        var tokens = Tokenize(groupPattern.Trim());
        var postfixTokens = ToPostfix(tokens);
        var rootNode = BuildNodeTree(postfixTokens, criterionCount);
        var rootGroup = rootNode.Group ?? new FilterGroup
        {
            Logic = FilterGroupLogic.And,
            Children = [rootNode]
        };

        return AppendMissingCriteria(rootGroup, criterionCount);
    }

    private static FilterGroup CreateAndGroup(List<int> criterionIndexes)
    {
        return new FilterGroup
        {
            Logic = FilterGroupLogic.And,
            Children = [.. criterionIndexes.Select(index => new FilterGroupNode
                {
                    CriterionIndex = index
                })]
        };
    }

    private static List<int> CreateCriterionIndexes(int criterionCount)
    {
        var criterionIndexes = new List<int>(criterionCount);

        for (var index = 0; index < criterionCount; index++)
        {
            criterionIndexes.Add(index);
        }

        return criterionIndexes;
    }

    private static List<Token> Tokenize(string groupPattern)
    {
        var tokens = new List<Token>();

        for (var index = 0; index < groupPattern.Length;)
        {
            var currentChar = groupPattern[index];

            if (char.IsWhiteSpace(currentChar))
            {
                index++;
                continue;
            }

            if (currentChar == '(')
            {
                tokens.Add(new Token(TokenType.LeftParenthesis, "(", index));
                index++;
                continue;
            }

            if (currentChar == ')')
            {
                tokens.Add(new Token(TokenType.RightParenthesis, ")", index));
                index++;
                continue;
            }

            if (currentChar == '{')
            {
                var closingBraceIndex = groupPattern.IndexOf('}', index + 1);
                if (closingBraceIndex < 0)
                {
                    throw CreateInvalidGroupException("Group pattern contains an unclosed criterion index.");
                }

                var rawIndex = groupPattern[(index + 1)..closingBraceIndex].Trim();
                if (!int.TryParse(rawIndex, out var criterionIndex) || criterionIndex < 0)
                {
                    throw CreateInvalidGroupException(
                        $"Invalid criterion index '{{{rawIndex}}}' in group pattern."
                    );
                }

                tokens.Add(
                    new Token(
                        TokenType.Criterion,
                        groupPattern[index..(closingBraceIndex + 1)],
                        index,
                        criterionIndex
                    )
                );

                index = closingBraceIndex + 1;
                continue;
            }

            if (TryReadKeyword(groupPattern, index, "AND", out var andKeyword))
            {
                tokens.Add(new Token(TokenType.And, andKeyword, index));
                index += andKeyword.Length;
                continue;
            }

            if (TryReadKeyword(groupPattern, index, "OR", out var orKeyword))
            {
                tokens.Add(new Token(TokenType.Or, orKeyword, index));
                index += orKeyword.Length;
                continue;
            }

            if (TryReadKeyword(groupPattern, index, "NOT", out var notKeyword))
            {
                tokens.Add(new Token(TokenType.Not, notKeyword, index));
                index += notKeyword.Length;
                continue;
            }

            throw CreateInvalidGroupException(
                $"Unsupported token near '{groupPattern[index..]}' in group pattern."
            );
        }

        return tokens;
    }

    private static List<Token> ToPostfix(List<Token> tokens)
    {
        var output = new List<Token>();
        var operators = new Stack<Token>();
        Token? previousToken = null;
        var expectingOperand = true;

        foreach (var token in tokens)
        {
            switch (token.Type)
            {
                case TokenType.Criterion:
                    if (!expectingOperand)
                    {
                        throw CreateInvalidGroupException(
                            $"Missing operator before criterion '{token.Text}'."
                        );
                    }

                    output.Add(token);
                    expectingOperand = false;
                    break;

                case TokenType.LeftParenthesis:
                    if (!expectingOperand)
                    {
                        throw CreateInvalidGroupException("Missing operator before '('.");
                    }

                    operators.Push(token);
                    expectingOperand = true;
                    break;

                case TokenType.RightParenthesis:
                    if (expectingOperand)
                    {
                        throw CreateInvalidGroupException("Unexpected ')' in group pattern.");
                    }

                    while (operators.Count > 0 && operators.Peek().Type != TokenType.LeftParenthesis)
                    {
                        output.Add(operators.Pop());
                    }

                    if (operators.Count == 0)
                    {
                        throw CreateInvalidGroupException("Unmatched ')' in group pattern.");
                    }

                    operators.Pop();
                    expectingOperand = false;
                    break;

                case TokenType.Not:
                    if (!expectingOperand || previousToken?.Type != TokenType.LeftParenthesis)
                    {
                        throw CreateInvalidGroupException(
                            "NOT must be wrapped in parentheses, for example '(NOT {1})'."
                        );
                    }

                    operators.Push(token);
                    expectingOperand = true;
                    break;

                case TokenType.And:
                case TokenType.Or:
                    if (expectingOperand)
                    {
                        throw CreateInvalidGroupException(
                            $"Operator '{token.Text}' is missing a left operand."
                        );
                    }

                    while (operators.Count > 0
                        && operators.Peek().Type != TokenType.LeftParenthesis
                        && GetPrecedence(operators.Peek().Type) >= GetPrecedence(token.Type))
                    {
                        output.Add(operators.Pop());
                    }

                    operators.Push(token);
                    expectingOperand = true;
                    break;
            }

            previousToken = token;
        }

        if (expectingOperand)
        {
            throw CreateInvalidGroupException("Group pattern cannot end with an operator.");
        }

        while (operators.Count > 0)
        {
            var operatorToken = operators.Pop();
            if (operatorToken.Type == TokenType.LeftParenthesis)
            {
                throw CreateInvalidGroupException("Unmatched '(' in group pattern.");
            }

            output.Add(operatorToken);
        }

        return output;
    }

    private static FilterGroupNode BuildNodeTree(
        List<Token> postfixTokens,
        int criterionCount
    )
    {
        var nodes = new Stack<FilterGroupNode>();

        foreach (var token in postfixTokens)
        {
            switch (token.Type)
            {
                case TokenType.Criterion:
                    ValidateCriterionIndex(token.CriterionIndex, criterionCount);
                    nodes.Push(
                        new FilterGroupNode
                        {
                            CriterionIndex = token.CriterionIndex
                        }
                    );
                    break;

                case TokenType.Not:
                    if (nodes.Count < 1)
                    {
                        throw CreateInvalidGroupException("NOT is missing its operand.");
                    }

                    var operand = nodes.Pop();
                    nodes.Push(
                        new FilterGroupNode
                        {
                            Group = new FilterGroup
                            {
                                Logic = FilterGroupLogic.Not,
                                Children = [operand]
                            }
                        }
                    );
                    break;

                case TokenType.And:
                case TokenType.Or:
                    if (nodes.Count < 2)
                    {
                        throw CreateInvalidGroupException(
                            $"Operator '{token.Text}' does not have enough operands."
                        );
                    }

                    var right = nodes.Pop();
                    var left = nodes.Pop();

                    nodes.Push(
                        new FilterGroupNode
                        {
                            Group = new FilterGroup
                            {
                                Logic = token.Type == TokenType.And
                                    ? FilterGroupLogic.And
                                    : FilterGroupLogic.Or,
                                Children = [left, right]
                            }
                        }
                    );
                    break;
            }
        }

        if (nodes.Count != 1)
        {
            throw CreateInvalidGroupException("Group pattern could not be reduced to one tree.");
        }

        return nodes.Pop();
    }

    private static FilterGroup AppendMissingCriteria(FilterGroup filterGroup, int criterionCount)
    {
        var referencedCriterionIndexes = new HashSet<int>();
        CollectReferencedCriterionIndexes(filterGroup, referencedCriterionIndexes);

        var missingCriterionIndexes = new List<int>();
        for (var index = 0; index < criterionCount; index++)
        {
            if (!referencedCriterionIndexes.Contains(index))
            {
                missingCriterionIndexes.Add(index);
            }
        }

        if (missingCriterionIndexes.Count == 0)
        {
            return filterGroup;
        }

        return new FilterGroup
        {
            Logic = FilterGroupLogic.And,
            Children =
            [
                new FilterGroupNode { Group = filterGroup },
                .. missingCriterionIndexes.Select(index => new FilterGroupNode
                {
                    CriterionIndex = index
                })
            ]
        };
    }

    private static void CollectReferencedCriterionIndexes(
        FilterGroup filterGroup,
        HashSet<int> referencedCriterionIndexes
    )
    {
        var groups = new Stack<FilterGroup>();
        groups.Push(filterGroup);

        while (groups.Count > 0)
        {
            var currentGroup = groups.Pop();

            foreach (var child in currentGroup.Children)
            {
                if (child.CriterionIndex.HasValue)
                {
                    referencedCriterionIndexes.Add(child.CriterionIndex.Value);
                }

                if (child.Group is not null)
                {
                    groups.Push(child.Group);
                }
            }
        }
    }

    private static void ValidateCriterionIndex(int criterionIndex, int criterionCount)
    {
        if (criterionIndex < 0 || criterionIndex >= criterionCount)
        {
            throw CreateInvalidGroupException(
                $"Criterion index '{criterionIndex}' is outside the available range."
            );
        }
    }

    private static int GetPrecedence(TokenType tokenType)
    {
        return tokenType switch
        {
            TokenType.Not => 3,
            TokenType.And => 2,
            TokenType.Or => 1,
            _ => 0
        };
    }

    private static bool TryReadKeyword(
        string source,
        int startIndex,
        string keyword,
        out string matchedKeyword
    )
    {
        matchedKeyword = string.Empty;

        if (startIndex + keyword.Length > source.Length)
        {
            return false;
        }

        var candidate = source.Substring(startIndex, keyword.Length);
        if (!string.Equals(candidate, keyword, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var endIndex = startIndex + keyword.Length;
        if (endIndex < source.Length && char.IsLetterOrDigit(source[endIndex]))
        {
            return false;
        }

        matchedKeyword = candidate;
        return true;
    }

    private static UnprocessableEntityException CreateInvalidGroupException(string internalMessage)
    {
        return new UnprocessableEntityException(internalMessage);
    }

    private enum TokenType
    {
        LeftParenthesis,
        RightParenthesis,
        Criterion,
        And,
        Or,
        Not
    }

    private readonly record struct Token(
        TokenType Type,
        string Text,
        int Position,
        int CriterionIndex = -1
    );
}