using FluentAssertions;
using HamsterWheel.HLinq.Request;
using HamsterWheel.HLinq.Tree.Selecting;
using HamsterWheel.HLinq.UnitTests.TestUtils.Dummies;

namespace HamsterWheel.HLinq.UnitTests.Pipeline.Applier;

/// <summary>
/// A join in a select answers the row like the same expression written by hand.
/// </summary>
partial class HLinqQueryApplierUnitTests
{
    [Fact]
    public void Apply_WhenJoinedMembers_ThenTextsAreJoinedLeftToRight()
    {
        //act
        var actual = ApplyNested("select[FullName=x.Name+\" \"+x.Name]");

        //assert
        Json(actual).Should().Be(Json(People().Select(p => new { FullName = p.Name + " " + p.Name })));
    }

    [Fact]
    public void Apply_WhenJoinWrittenAsTextInLinq_ThenSameAnswerAsTheCompiler()
    {
        //act
        var actual = ApplyMath("select[FullName=x.Name+\"x\"+x.Int]", MathRows());

        //assert
        Json(actual).Should().Be(Json(MathRows().Select(e => new { FullName = e.Name + "x" + e.Int })));
    }

    [Fact]
    public void Apply_WhenJoinAmongOtherMembers_ThenEveryMemberIsAnswered()
    {
        //act
        var actual = ApplyMath("select[FullName=x.Name+\" \"+x.Text,x.Id]", MathRows());

        //assert
        Json(actual).Should().Be(Json(MathRows().Select(e => new { FullName = e.Name + " " + e.Text, e.Id })));
    }

    [Fact]
    public void Apply_WhenJoinHangsOnCalls_ThenCallsAreBuiltInTheJoinToo()
    {
        //act
        var actual = ApplyMath("select[U=x.Name.ToUpper()+\"!\"]", MathRows());

        //assert
        Json(actual).Should().Be(Json(MathRows().Select(e => new { U = e.Name.ToUpper() + "!" })));
    }

    [Fact]
    public void Apply_WhenJoinAddsNumbers_ThenTheSumIsTheAnswer()
    {
        //act
        var actual = ApplyMath("select[N=x.Int+x.Long+x.NullableDecimal]", MathRows());

        //assert
        Json(actual).Should().Be(Json(MathRows()
            .Select(e => new { N = e.Int + e.Long + e.NullableDecimal })));
    }

    [Fact]
    public void Apply_WhenJoinReadsAForeignText_ThenTheForeignValueJoinsAsText()
    {
        //act
        var actual = ApplyMath("select[D=x.DateTime+\"x\"]", MathRows());

        //assert
        Json(actual).Should().Be(Json(MathRows().Select(e => new { D = e.DateTime + "x" })));
    }

    [Fact]
    public void Apply_WhenJoinInsideNestedSelect_ThenPiecesReadTheirOwnScopes()
    {
        //act
        var actual = ApplyNested("select[A=x.Addresses.select(City=a.City+\" \"+x.Name)]");

        //assert
        Json(actual).Should().Be(Json(People().Select(p => new
        {
            A = p.Addresses.Select(a => new { City = a.City + " " + p.Name }).ToList()
        })));
    }

    [Fact]
    public void Apply_WhenJoinUnnamed_ThenRefusedWithTheReason()
    {
        //act
        var act = () => ApplyNested("select[x.Name+\" \"+x.Name]");

        //assert
        act.Should().Throw<PropertyAssignment.MathExpressionNeedsANameException>();
    }

    [Fact]
    public void Apply_WhenJoinAddsWhatNothingAdds_ThenRefusedWithTheReason()
    {
        //act
        //in Linq does not compile either (adding days to a date is what a call is for)
        var act = () => ApplyMath("select[N=x.DateTime+x.Long]", MathRows());

        //assert
        act.Should()
            .Throw<PropertyAssignment.MathOperandTypesCannotBeOperatedException>();
    }

    [Fact]
    public void Apply_WhenJoinReadsAnEnumeration_ThenRefusedWithTheReason()
    {
        //act
        var act = () => ApplyMath("select[E=x.Enum+\"x\"]", MathRows());

        //assert
        act.Should().Throw<PropertyAssignment.MathOperandIsAnEnumerationException>();
    }

    [Fact]
    public void Apply_WhenWrittenNumbersAdd_ThenTheSumIsTheAnswer()
    {
        //act
        var actual = ApplyMath("select[N=x.Int+1]", MathRows());

        //assert
        Json(actual).Should().Be(Json(MathRows().Select(e => new { N = e.Int + 1 })));
    }

    [Fact]
    public void Apply_WhenOperatorsMix_ThenMultiplicationBindsLikeCSharp()
    {
        //act
        var actual = ApplyMath("select[N=x.Int+x.Long*2-x.Int]", MathRows());

        //assert
        Json(actual).Should().Be(Json(MathRows().Select(e => new { N = e.Int + e.Long * 2 - e.Int })));
    }

    [Fact]
    public void Apply_WhenNumbersDivide_ThenTheQuotientIsTheAnswer()
    {
        //act
        var actual = ApplyMath("select[N=x.Long/x.Int]", MathRows());

        //assert
        Json(actual).Should().Be(Json(MathRows().Select(e => new { N = e.Long / e.Int })));
    }

    [Fact]
    public void Apply_WhenDecimalAddsADouble_ThenTheSumIsADouble()
    {
        //act
        //meets it: the sum is the number the same expression written by hand answers
        var actual = ApplyMath("select[N=x.Decimal+x.Double]", MathRows());

        //assert
        Json(actual).Should().Be(Json(MathRows().Select(e => new { N = (double)e.Decimal + e.Double })));
    }

    [Fact]
    public void Apply_WhenDecimalAddsAFloat_ThenTheSumIsADouble()
    {
        //act
        var actual = ApplyMath("select[N=x.Decimal+x.Float]", MathRows());

        //assert
        Json(actual).Should().Be(Json(MathRows().Select(e => new { N = (double)e.Decimal + e.Float })));
    }

    [Fact]
    public void Apply_WhenNullableDecimalAddsADouble_ThenTheSumIsANullableDouble()
    {
        //act
        var actual = ApplyMath("select[N=x.NullableDecimal+x.Double]", MathRows());

        //assert
        Json(actual).Should().Be(
            Json(MathRows().Select(e => new { N = (double?)e.NullableDecimal + e.Double })));
    }

    [Fact]
    public void Apply_WhenADecimalIsSubtractedFromADouble_ThenTheAnswerIsADouble()
    {
        //act
        var actual = ApplyMath("select[N=x.Decimal-x.Double]", MathRows());

        //assert
        Json(actual).Should().Be(Json(MathRows().Select(e => new { N = (double)e.Decimal - e.Double })));
    }

    [Fact]
    public void Apply_WhenADecimalIsDividedByADouble_ThenTheAnswerIsADouble()
    {
        //act
        var actual = ApplyMath("select[N=x.Decimal/x.Double]", MathRows());

        //assert
        Json(actual).Should().Be(Json(MathRows().Select(e => new { N = (double)e.Decimal / e.Double })));
    }

    [Fact]
    public void Apply_WhenADecimalMeetsAWrittenFraction_ThenTheSumIsADouble()
    {
        //act
        //`double`, and the pair is met as any other decimal-and-double pair; the hand-written
        //answer spells that meeting as the cast the query's author has no way to write
        var actual = ApplyMath("select[N=x.Decimal+1.5]", MathRows());

        //assert
        Json(actual).Should().Be(Json(MathRows().Select(e => new { N = (double)e.Decimal + 1.5 })));
    }

    [Fact]
    public void Apply_WhenADecimalMeetsAWrittenInteger_ThenTheSumKeepsItsDecimal()
    {
        //act
        var actual = ApplyMath("select[N=x.Decimal+2]", MathRows());

        //assert
        Json(actual).Should().Be(Json(MathRows().Select(e => new { N = e.Decimal + 2m })));
    }

    [Fact]
    public void Apply_WhenADecimalAddsIntegrals_ThenTheSumKeepsItsDecimal()
    {
        //act
        var actual = ApplyMath("select[N=x.Int+x.Long+x.NullableDecimal]", MathRows());

        //assert
        Json(actual).Should().Be(Json(MathRows()
            .Select(e => new { N = e.Int + e.Long + e.NullableDecimal })));
    }

    [Fact]
    public void Apply_WhenADecimalOfItsTailAddsADouble_ThenTheAnswerIsTheNearestDouble()
    {
        //act
        //gone from the sum, and that is the sum the same expression written by hand answers
        var lossy = new[]
        {
            new DummyEntity("Loss")
            {
                Decimal = 9999999999999999.99m,
                Double = 1e+16
            }
        };

        var actual = ApplyMath("select[N=x.Decimal+x.Double]", lossy);

        //assert
        Json(actual).Should().Be(Json(lossy.Select(e => new { N = (double)e.Decimal + e.Double })));
    }

    [Fact]
    public void Apply_WhenUintAddsAByte_ThenTheSumIsAUint()
    {
        //act
        var actual = ApplyMath("select[N=x.Uint+x.Byte]", MathRows());

        //assert
        Json(actual).Should().Be(Json(MathRows().Select(e => new { N = e.Uint + e.Byte })));
    }

    [Fact]
    public void Apply_WhenUintAddsAnInt_ThenTheSumIsALong()
    {
        //act
        var actual = ApplyMath("select[N=x.Uint+x.Int]", MathRows());

        //assert
        Json(actual).Should().Be(Json(MathRows().Select(e => new { N = e.Uint + e.Int })));
    }

    [Fact]
    public void Apply_WhenAnIntegralAddsAUlong_ThenRefusedWithTheReason()
    {
        //act
        //pair has no meeting to state and the fold refuses it as any pair it cannot operate on
        var act = () => ApplyMath("select[N=x.Long+x.Unsigned]", MathRows());

        //assert
        act.Should()
            .Throw<PropertyAssignment.MathOperandTypesCannotBeOperatedException>();
    }

    [Fact]
    public void Apply_WhenADecimalAddsAUlong_ThenTheSumKeepsItsDecimal()
    {
        //act
        //holds every value a ulong has: C# answers the same sum written by hand a decimal
        var actual = ApplyMath("select[N=x.Decimal+x.Unsigned]", MathRows());

        //assert
        Json(actual).Should().Be(Json(MathRows().Select(e => new { N = e.Decimal + e.Unsigned })));
    }

    [Fact]
    public void Apply_WhenTextOpensTheExpression_ThenTheGroupSaysArithmetic()
    {
        //act
        //were; a group is where the query says: read what I wrote between them as an expression
        var actual = ApplyMath("select[G=(\"Mr \"+x.Name)]", MathRows());

        //assert
        Json(actual).Should().Be(Json(MathRows().Select(e => new { G = "Mr " + e.Name })));
    }

    [Fact]
    public void Apply_WhenGroupHoldsWrittenNumbers_ThenTheyAreOperandsAndNotDates()
    {
        //act
        //is two subtractions, because the group is where the query says arithmetic and a bare
        //date is what the free value grammar of a clause reads
        var actual = ApplyMath("select[N=(2022-12-31)]", MathRows());

        //assert
        Json(actual).Should().Be(Json(MathRows().Select(_ => new { N = 2022 - 12 - 31 })));
    }

    [Fact]
    public void Apply_WhenGroupMultiplies_ThenTheGroupIsFoldedFirst()
    {
        //act
        var actual = ApplyMath("select[N=(x.Int+1)*2]", MathRows());

        //assert
        Json(actual).Should().Be(Json(MathRows().Select(e => new { N = (e.Int + 1) * 2 })));
    }

    [Fact]
    public void Validate_WhenJoinUnnamed_ThenTheAnswerNamesItself()
    {
        //act
        var errors = HLinqQuery<DummyEntity>.Validate(ServicesCollection.BinderDependenciesBag,
            "select[x.Name%2B%22%20%22%2Bx.Text]");

        //assert
        var error = errors.Should().ContainSingle().Which;
        error.RootKeyword.Should().Be("select");
    }

    [Fact]
    public void Validate_WhenJoinAddsWhatNothingAdds_ThenTheAnswerNamesItself()
    {
        //act
        var errors = HLinqQuery<DummyEntity>.Validate(ServicesCollection.BinderDependenciesBag,
            "select[N=x.DateTime%2Bx.Long]");

        //assert
        errors.Should().ContainSingle();
    }

    private static DummyEntity[] MathRows() =>
    [
        new("Ann")
        {
            Text = "Lee",
            Int = 2,
            Long = 5,
            Decimal = 12.5m,
            Double = 3.5,
            Float = 7.25f,
            Byte = 4,
            Uint = 7u,
            Unsigned = 3UL,
            NullableDecimal = 1.5m,
            Enum = DummyEnum.One,
            DateTime = new DateTime(2020, 1, 2)
        },
        new("Bob")
        {
            Text = "Day",
            Int = 7,
            Long = 11,
            Decimal = 2.75m,
            Double = 0.5,
            Float = 3.5f,
            Byte = 200,
            Uint = 9u,
            Unsigned = 5UL,
            NullableDecimal = null,
            Enum = DummyEnum.Two,
            DateTime = new DateTime(2021, 3, 4)
        }
    ];

    /// <summary>
    /// The join written as the language writes it, percent-encoding and all.
    /// </summary>
    private object ApplyMath(string queryString, IEnumerable<DummyEntity> entities)
    {
        var hlinqQuery = new HLinqQuery<DummyEntity>
        {
            SourceQueryString = queryString
        };
        _parser.Parse(hlinqQuery, _tokenizer.Tokenize(queryString));
        return _sut.Apply(entities.AsQueryable(), hlinqQuery);
    }
}
