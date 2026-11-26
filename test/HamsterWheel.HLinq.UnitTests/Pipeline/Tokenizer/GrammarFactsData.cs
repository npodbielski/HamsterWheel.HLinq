namespace HamsterWheel.HLinq.UnitTests.Pipeline.Tokenizer;

/// <summary>
/// What the invariant grammar answers, as this version of the rules answers it.
/// </summary>
public static class GrammarFactsData
{
    public const string Roots = """
    CountRoot: -
    GroupByRoot: -
    OrderByDescendingRoot: -
    OrderByRoot: -
    SelectRoot: -
    SkipRoot: -
    TakeRoot: -
    ThenByDescendingRoot: -
    ThenByRoot: -
    WhereRoot: -
    """;

    public const string Parents = """
    ComparisonConst: Condition
    ComparisonOperation: Condition
    Condition: ConditionGroup WhereRoot
    ConditionGroup: ConditionGroup WhereRoot
    CountRoot: -
    EntityLeaf: Condition OrderByDescendingRoot OrderByRoot ThenByDescendingRoot ThenByRoot
    GroupByRoot: -
    InitializerConstantValue: PropertyAssignment
    InitializerPropertyName: PropertyAssignment
    InnerProperty: MathGroup MathTerm PropertyAssignment
    MathConstant: MathGroup MathTerm
    MathGroup: Condition MathGroup MathTerm OrderByDescendingRoot OrderByRoot PropertyAssignment ThenByDescendingRoot ThenByRoot
    MathTerm: Condition MathGroup OrderByDescendingRoot OrderByRoot PropertyAssignment ThenByDescendingRoot ThenByRoot
    Method: Condition MathGroup MathTerm OrderByDescendingRoot OrderByRoot PropertyAssignment ThenByDescendingRoot ThenByRoot
    MethodConstParam: Method
    NestedSelect: PropertyAssignment
    OrderByDescendingRoot: -
    OrderByRoot: -
    Property: Condition MathGroup MathTerm Method NestedSelect OrderByDescendingRoot OrderByRoot PropertyAssignment ThenByDescendingRoot ThenByRoot
    PropertyAssignment: GroupByRoot NestedSelect SelectRoot
    SelectRoot: -
    SkipOrTakeConstant: SkipOrTakeRange SkipRoot TakeRoot
    SkipOrTakeRange: TakeRoot
    SkipRoot: -
    TakeRoot: -
    ThenByDescendingRoot: -
    ThenByRoot: -
    WhereRoot: -
    """;

    public const string Words = """
    And: &&
    Assignment: =
    Comma: ,
    Count: count
    Divide: /
    Dot: .
    Entity: x
    Equality: ==
    GreaterOrEqualThan: >=
    GreaterThan: >
    GroupBy: groupBy
    Inequality: !=
    LeftCircleBracket: (
    LeftSquareBracket: [
    LessOrEqualThan: <=
    LessThan: <
    Minus: -
    Multiply: *
    Or: ||
    OrderBy: orderBy
    OrderByDescending: orderByDescending
    Plus: +
    RangeOperator: ..
    RightCircleBracket: )
    RightSquareBracket: ]
    Select: select
    Skip: skip
    Take: take
    ThenBy: thenBy
    ThenByDescending: thenByDescending
    Where: where
    """;

    public const string CanOpenQuery = """
    Count GroupBy OrderBy OrderByDescending Select Skip Take ThenBy ThenByDescending Where
    """;

    public const string StandsAfter = """
    And: MathOperand NameOrValue PropertyName RightCircleBracket
    Assignment: NameOrValue
    Comma: MathOperand NameOrValue PropertyName RightCircleBracket
    Count: -
    Divide: MathOperand PropertyName RightCircleBracket
    Dot: Entity PropertyName RightSquareBracket SubEntity
    Entity: And Assignment Comma Divide Equality GreaterOrEqualThan GreaterThan Inequality LeftCircleBracket LeftSquareBracket LessOrEqualThan LessThan Minus Multiply Or Plus
    Equality: Entity MathOperand NameOrValue PropertyName RightCircleBracket
    GreaterOrEqualThan: Entity MathOperand NameOrValue PropertyName RightCircleBracket
    GreaterThan: Entity MathOperand NameOrValue PropertyName RightCircleBracket
    GroupBy: -
    Inequality: Entity MathOperand NameOrValue PropertyName RightCircleBracket
    LeftCircleBracket: And Assignment Divide Equality GreaterOrEqualThan GreaterThan Inequality LeftCircleBracket LeftSquareBracket LessOrEqualThan LessThan MethodName Minus Multiply Or Plus
    LeftSquareBracket: Count GroupBy OrderBy OrderByDescending Select Skip Take ThenBy ThenByDescending Where
    LessOrEqualThan: Entity MathOperand NameOrValue PropertyName RightCircleBracket
    LessThan: Entity MathOperand NameOrValue PropertyName RightCircleBracket
    MathOperand: Divide Minus Multiply Plus
    MethodName: Assignment Divide Dot Equality GreaterOrEqualThan GreaterThan Inequality LeftSquareBracket LessOrEqualThan LessThan Minus Multiply Plus
    Minus: MathOperand PropertyName RightCircleBracket
    Multiply: MathOperand PropertyName RightCircleBracket
    NameOrValue: RangeOperator
    Or: MathOperand NameOrValue PropertyName RightCircleBracket
    OrderBy: -
    OrderByDescending: -
    Plus: MathOperand PropertyName RightCircleBracket
    PropertyName: -
    RangeOperator: -
    RightCircleBracket: LeftCircleBracket MathOperand NameOrValue PropertyName RightCircleBracket
    RightSquareBracket: LeftSquareBracket MathOperand NameOrValue PropertyName RangeOperator RightCircleBracket
    Select: -
    Skip: -
    SubEntity: LeftCircleBracket
    Take: -
    ThenBy: -
    ThenByDescending: -
    Where: -
    """;

    public const string StandsBefore = """
    And: Entity LeftCircleBracket
    Assignment: Entity LeftCircleBracket MethodName
    Comma: Entity
    Count: LeftSquareBracket
    Divide: Entity LeftCircleBracket MathOperand MethodName
    Dot: MethodName
    Entity: Dot Equality GreaterOrEqualThan GreaterThan Inequality LessOrEqualThan LessThan
    Equality: Entity LeftCircleBracket MethodName
    GreaterOrEqualThan: Entity LeftCircleBracket MethodName
    GreaterThan: Entity LeftCircleBracket MethodName
    GroupBy: LeftSquareBracket
    Inequality: Entity LeftCircleBracket MethodName
    LeftCircleBracket: Entity RightCircleBracket SubEntity
    LeftSquareBracket: Entity LeftCircleBracket MethodName RightSquareBracket
    LessOrEqualThan: Entity LeftCircleBracket MethodName
    LessThan: Entity LeftCircleBracket MethodName
    MathOperand: And Comma Divide Equality GreaterOrEqualThan GreaterThan Inequality LessOrEqualThan LessThan Minus Multiply Or Plus RightCircleBracket RightSquareBracket
    MethodName: LeftCircleBracket
    Minus: Entity LeftCircleBracket MathOperand MethodName
    Multiply: Entity LeftCircleBracket MathOperand MethodName
    NameOrValue: And Assignment Comma Equality GreaterOrEqualThan GreaterThan Inequality LessOrEqualThan LessThan Or RightCircleBracket RightSquareBracket
    Or: Entity LeftCircleBracket
    OrderBy: LeftSquareBracket
    OrderByDescending: LeftSquareBracket
    Plus: Entity LeftCircleBracket MathOperand MethodName
    PropertyName: And Comma Divide Dot Equality GreaterOrEqualThan GreaterThan Inequality LessOrEqualThan LessThan Minus Multiply Or Plus RightCircleBracket RightSquareBracket
    RangeOperator: NameOrValue RightSquareBracket
    RightCircleBracket: And Comma Divide Equality GreaterOrEqualThan GreaterThan Inequality LessOrEqualThan LessThan Minus Multiply Or Plus RightSquareBracket
    RightSquareBracket: Dot
    Select: LeftSquareBracket
    Skip: LeftSquareBracket
    SubEntity: Dot
    Take: LeftSquareBracket
    ThenBy: LeftSquareBracket
    ThenByDescending: LeftSquareBracket
    Where: LeftSquareBracket
    """;

    public const string ClauseRoots = """
    And: Count GroupBy OrderBy OrderByDescending Select Skip Take ThenBy ThenByDescending Where
    Assignment: Select
    Comma: Count GroupBy OrderBy OrderByDescending Select Skip Take ThenBy ThenByDescending Where
    Count: Count GroupBy OrderBy OrderByDescending Select Skip Take ThenBy ThenByDescending Where
    Divide: OrderBy OrderByDescending Select ThenBy ThenByDescending Where
    Dot: Count GroupBy OrderBy OrderByDescending Select Skip Take ThenBy ThenByDescending Where
    Entity: Count GroupBy OrderBy OrderByDescending Select Skip Take ThenBy ThenByDescending Where
    Equality: Count GroupBy OrderBy OrderByDescending Skip Take ThenBy ThenByDescending Where
    GreaterOrEqualThan: Count GroupBy OrderBy OrderByDescending Skip Take ThenBy ThenByDescending Where
    GreaterThan: Count GroupBy OrderBy OrderByDescending Skip Take ThenBy ThenByDescending Where
    GroupBy: Count GroupBy OrderBy OrderByDescending Select Skip Take ThenBy ThenByDescending Where
    Inequality: Count GroupBy OrderBy OrderByDescending Skip Take ThenBy ThenByDescending Where
    LeftCircleBracket: Count GroupBy OrderBy OrderByDescending Select Skip Take ThenBy ThenByDescending Where
    LeftSquareBracket: Count GroupBy OrderBy OrderByDescending Select Skip Take ThenBy ThenByDescending Where
    LessOrEqualThan: Count GroupBy OrderBy OrderByDescending Skip Take ThenBy ThenByDescending Where
    LessThan: Count GroupBy OrderBy OrderByDescending Skip Take ThenBy ThenByDescending Where
    MathOperand: OrderBy OrderByDescending Select ThenBy ThenByDescending Where
    MethodName: Count GroupBy OrderBy OrderByDescending Select Skip Take ThenBy ThenByDescending Where
    Minus: OrderBy OrderByDescending Select ThenBy ThenByDescending Where
    Multiply: OrderBy OrderByDescending Select ThenBy ThenByDescending Where
    NameOrValue: Count GroupBy OrderBy OrderByDescending Select Skip Take ThenBy ThenByDescending Where
    Or: Count GroupBy OrderBy OrderByDescending Select Skip Take ThenBy ThenByDescending Where
    OrderBy: Count GroupBy OrderBy OrderByDescending Select Skip Take ThenBy ThenByDescending Where
    OrderByDescending: Count GroupBy OrderBy OrderByDescending Select Skip Take ThenBy ThenByDescending Where
    Plus: OrderBy OrderByDescending Select ThenBy ThenByDescending Where
    PropertyName: Count GroupBy OrderBy OrderByDescending Select Skip Take ThenBy ThenByDescending Where
    RangeOperator: Count GroupBy OrderBy OrderByDescending Select Skip Take ThenBy ThenByDescending Where
    RightCircleBracket: Count GroupBy OrderBy OrderByDescending Select Skip Take ThenBy ThenByDescending Where
    RightSquareBracket: Count GroupBy OrderBy OrderByDescending Select Skip Take ThenBy ThenByDescending Where
    Select: Count GroupBy OrderBy OrderByDescending Select Skip Take ThenBy ThenByDescending Where
    Skip: Count GroupBy OrderBy OrderByDescending Select Skip Take ThenBy ThenByDescending Where
    SubEntity: Select
    Take: Count GroupBy OrderBy OrderByDescending Select Skip Take ThenBy ThenByDescending Where
    ThenBy: Count GroupBy OrderBy OrderByDescending Select Skip Take ThenBy ThenByDescending Where
    ThenByDescending: Count GroupBy OrderBy OrderByDescending Select Skip Take ThenBy ThenByDescending Where
    Where: Count GroupBy OrderBy OrderByDescending Select Skip Take ThenBy ThenByDescending Where
    """;

}