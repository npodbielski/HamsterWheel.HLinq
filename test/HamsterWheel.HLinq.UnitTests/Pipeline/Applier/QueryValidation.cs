using FluentAssertions;
using HamsterWheel.HLinq.Diagnostics;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Request;
using HamsterWheel.HLinq.UnitTests.TestUtils.Dummies;
using Microsoft.Extensions.DependencyInjection;

namespace HamsterWheel.HLinq.UnitTests.Pipeline.Applier;

/// <summary>
/// A query never run answers one entry per thing wrong, each named by a code.
/// </summary>
partial class HLinqQueryApplierUnitTests
{
    private static DummyEntity[] NoRows => [];

    [Theory]
    [InlineData("where[x.Nope==1]", HLinqErrorCode.UnknownMember, "where", "Nope")]
    [InlineData("where[x.Nested.Nope==1]", HLinqErrorCode.UnknownMember, "where", "Nested.Nope")]
    [InlineData("select[x.Nope]", HLinqErrorCode.UnknownMember, "select", "Nope")]
    [InlineData("orderBy[x.Nope]", HLinqErrorCode.UnknownMember, "orderBy", "Nope")]
    [InlineData("groupBy[x.Nope]", HLinqErrorCode.UnknownMember, "groupBy", "Nope")]
    [InlineData("where[x.Name==x.Int]", HLinqErrorCode.InvalidComparison, "where", "String vs Int32")]
    [InlineData("where[x.Name>x.Text]", HLinqErrorCode.InvalidComparison, "where", "String vs String")]
    [InlineData("where[1==1]", HLinqErrorCode.ConstantOnlyComparison, "where", null)]
    [InlineData("where[x.Count()]", HLinqErrorCode.AggregateNeedsGroup, "where", "Count")]
    [InlineData("where[x.Nope(x.AlsoNope)]", HLinqErrorCode.NotAnAggregate, "where", "Nope")]
    [InlineData("where[x.Name.Nope(1)]", HLinqErrorCode.InvalidMethodParameters, "where", "Name.Nope")]
    [InlineData("take[-5]", HLinqErrorCode.InvalidPagingValue, "take", "take[-5]")]
    [InlineData("skip[-1]", HLinqErrorCode.InvalidPagingValue, "skip", "skip[-1]")]
    [InlineData("take[abc]", HLinqErrorCode.ImpossibleConversion, "take", "abc")]
    [InlineData("select[x.Name,x.Name]", HLinqErrorCode.DuplicateMemberName, null, "Name")]
    [InlineData("foo[]", HLinqErrorCode.InvalidQuery, null, "foo[]")]
    [InlineData("where[10 20]", HLinqErrorCode.InvalidQuery, null, "10 20]")]
    [InlineData("culture=xx&where[x.Name==a]", HLinqErrorCode.UnknownCulture, null, "xx")]
    public void Validate_WhenTheQueryIsInvalid_ThenTheAnswerNamesItself(
        string query, HLinqErrorCode code, string? rootKeyword, string? text)
    {
        //act
        var errors = HLinqQuery<DummyEntity>.Validate(ServicesCollection.BinderDependenciesBag, query);

        //assert
        var error = errors.Should().ContainSingle().Which;
        error.ErrorCode.Should().Be(code.ToErrorString());
        error.RootKeyword.Should().Be(rootKeyword);

        if (text is not null)
        {
            error.Text.Should().Be(text);
        }
    }

    [Theory]
    [InlineData("where[x.Name==x.Name]")]
    [InlineData("where[x.Decimal==x.Double]")]
    [InlineData("where[x.Enum==x.Int]")]
    [InlineData("select[x.Int,x.DateTime]")]
    [InlineData("where[x.Name==12312]")]
    [InlineData("where[x.Flag]")]
    [InlineData("where[x.Name==x.Name&&x.Int==x.Int].orderBy[x.Int]")]
    [InlineData("groupBy[x.Text].select[C=x.Count(),x.Int]")]
    [InlineData("count[]")]
    [InlineData("")]
    public void Validate_WhenQueryBuilds_ThenNothingIsAnswered(string query)
    {
        //act
        var errors = HLinqQuery<DummyEntity>.Validate(ServicesCollection.BinderDependenciesBag, query);

        //assert
        //which is why it answers over a type with no rows at all
        errors.Should().BeEmpty();
        HLinqQuery<DummyEntity>.Validate(ServicesCollection.BinderDependenciesBag, query).Should().BeEmpty();
    }

    [Fact]
    public void Validate_WhenFourConditionsOfOneRootAreWrong_ThenAllFourAreAnsweredAtOnce()
    {
        //act
        var errors = HLinqQuery<DummyEntity>.Validate(ServicesCollection.BinderDependenciesBag,
            "where[x.Nope==1&&x.Texto==2&&x.Into>3&&x.Namee==a]");

        //assert
        errors.Select(e => e.Text).Should().Equal("Nope", "Texto", "Into", "Namee");
        errors.Should().OnlyContain(e => e.ErrorCode == HLinqErrorCode.UnknownMember.ToErrorString());
        errors.Should().OnlyContain(e => e.RootKeyword == "where");
    }

    [Fact]
    public void Validate_WhenARootChangesResponseAndIsInvalid_ThenItsOwnErrorIsTheLastAnswer()
    {
        //act
        //root after it to be read against and naming its members would be a guess
        var afterSelect = HLinqQuery<DummyEntity>.Validate(ServicesCollection.BinderDependenciesBag,
            "where[x.Nope==1].select[x.Texto].orderBy[x.Into]");
        var afterGroupBy = HLinqQuery<DummyEntity>.Validate(ServicesCollection.BinderDependenciesBag,
            "where[x.Nope==1].groupBy[x.Texto].where[x.Into==1]");

        //assert
        afterSelect.Select(e => (e.RootKeyword, e.Text)).Should().Equal(("where", "Nope"),
            ("select", "Texto"));
        afterGroupBy.Select(e => (e.RootKeyword, e.Text)).Should().Equal(("where", "Nope"),
            ("groupBy", "Texto"));
    }

    [Fact]
    public void Apply_WhenTheRootsAfterTheInvalidOneAreWrongToo_ThenTheExceptionCarriesEveryAnswer()
    {
        //arrange
        //what the query wrote and is answered too
        const string query = "where[x.Nope==1].where[x.Texto==2].take[abc]";
        var hlinqQuery = Bind(query);

        //act
        var act = () => _sut.Apply(NoRows.AsQueryable(), hlinqQuery);

        //assert
        //one by name, but the roots read before it was thrown travel on it instead of costing a
        //round trip each
        var exception = act.Should().Throw<InvalidPropertyPathException>().Which;
        exception.InvalidQueryChunk.Should().Be("Nope");
        exception.Errors.Select(e => (e.RootKeyword, e.ErrorCode, e.Text)).Should().Equal(
            ("where", "unknown-member", "Nope"),
            ("where", "unknown-member", "Texto"),
            ("take", "impossible-conversion", "abc"));
    }

    [Fact]
    public void Apply_WhenOneConditionOfARootIsInvalid_ThenNoneOfThatRootReachesTheData()
    {
        //arrange
        //the one condition that survived would answer with rows the query never asked for
        var rows = new[] { new DummyEntity("ann") { Int = 1 }, new DummyEntity("bob") { Int = 2 } };
        var hlinqQuery = Bind("where[x.Int>0&&x.Nope==1]");

        //act
        var act = () => _sut.Apply(rows.AsQueryable(), hlinqQuery);

        //assert
        act.Should().Throw<InvalidPropertyPathException>();
    }

    [Fact]
    public void Validate_WhenARootAfterAnInvalidOneBuildsOnWhatNeverGotBuilt_ThenTheAnswerIsTheException()
    {
        //arrange
        //that was never produced, and reaches something no exception names: a query that is wrong is
        //still answered with what the caller can fix, and not with an internal error
        const string query = "groupBy[x.Nope].select[C=x.Count()]";

        //act
        var errors = HLinqQuery<DummyEntity>.Validate(ServicesCollection.BinderDependenciesBag, query);

        //assert
        errors.Select(e => (e.ErrorCode, e.Text)).Should().Equal(("unknown-member", "Nope"));
    }

    [Fact]
    public void Validate_WhenTheAnswerNamesAMember_ThenItSaysWhatTheTypeOffers()
    {
        //act
        var errors = HLinqQuery<DummyEntity>.Validate(ServicesCollection.BinderDependenciesBag,
            "where[x.Nope==1]");

        //assert
        errors.Single().Examples.Should().Contain("name").And.Contain("int");
    }

    [Fact]
    public void Validate_WhenAComparisonFitsNeitherSide_ThenItNamesTheTypesItStoodBetween()
    {
        //act
        var errors = HLinqQuery<DummyEntity>.Validate(ServicesCollection.BinderDependenciesBag,
            "where[x.Name==x.Int]");

        //assert
        errors.Single().Message.Should().Be(
            "Cannot compare a value of type 'String' with a value of type 'Int32' - use properties of " +
            "the same type, or compare a property with a value of its type.");
    }

    [Fact]
    public void Validate_WhenTheQueryStandsAfterAGrouping_ThenItIsBuiltAgainstTheGroup()
    {
        //act
        var errors = HLinqQuery<DummyEntity>.Validate(ServicesCollection.BinderDependenciesBag,
            "groupBy[x.Text].select[C=x.Count(),x.Nope]");

        //assert
        //with nothing to fold, so only the member that is not there is answered
        errors.Should().ContainSingle().Which.Text.Should().Be("Nope");
    }

    [Fact]
    public void Validate_WhenAQueryIsLongerThanTheHostLets_ThenItIsRejectedBeforeItIsRead()
    {
        //arrange
        var query = "where[x.Name==" + new string('a', 30) + "]";

        //act
        var errors = ValidateWith(new HLinqOptions { MaximumQueryLength = 20 }, query);

        //assert
        var error = errors.Should().ContainSingle().Which;
        error.ErrorCode.Should().Be("query-too-long");
        error.Text.Should().Be(query.Length.ToString());
        error.Examples.Should().Equal("20");
    }

    [Fact]
    public void Validate_WhenAHostRaisesTheLengthLimit_ThenTheLongerQueryIsRead()
    {
        //arrange
        var query = "where[x.Name==" + new string('a', 30) + "]";

        //act
        var errors = ValidateWith(new HLinqOptions { MaximumQueryLength = 1000 }, query);

        //assert
        errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("select[a=x.Name,b=x.Text,c=x.Int]")]
    [InlineData("select[x.Children.select(a=x.Name,b=x.Text,c=x.Int)]")]
    public void Validate_WhenARootWritesMoreMembersThanTheHostLets_ThenTheQueryIsInvalid(string query)
    {
        //act
        var errors = ValidateWith(new HLinqOptions { MaximumNumberOfProperties = 2 }, query);

        //assert
        //own whose members are columns of the answer too. The error is of the query and not of a
        //root, so it names no root: nothing of the query is read once the query is invalid.
        var error = errors.Should().ContainSingle().Which;
        error.ErrorCode.Should().Be(HLinqErrorCode.TooManyProperties.ToErrorString());
        error.RootKeyword.Should().BeNull();
        error.Text.Should().Be("3");
        error.Examples.Should().Equal("2");
    }

    [Fact]
    public void Validate_WhenTheQueryIsTooWide_ThenNothingOfItIsRead()
    {
        //act
        //too long is: the member that is a typo is never reached and is not answered
        var errors = ValidateWith(new HLinqOptions { MaximumNumberOfProperties = 1 },
            "select[a=x.Name,b=x.Text].where[x.Nope==1]");

        //assert
        errors.Should().ContainSingle().Which.ErrorCode.Should().Be(HLinqErrorCode.TooManyProperties.ToErrorString());
    }

    [Fact]
    public void Validate_WhenARootWritesAsManyMembersAsTheHostLets_ThenItIsRead()
    {
        //act
        var errors = ValidateWith(new HLinqOptions { MaximumNumberOfProperties = 2 }, "select[a=x.Name,b=x.Text]");

        //assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Apply_WhenARootAfterAMatchingOneIsInvalid_ThenNoRowIsRead()
    {
        //arrange
        var rows = new[] { new DummyEntity("ann") { Int = 1 }, new DummyEntity("bob") { Int = 2 } };
        var hlinqQuery = Bind("where[x.Int>0].select[x.Nope]");

        //act
        var act = () => _sut.Apply(rows.AsQueryable(), hlinqQuery);

        //assert
        //an invalid root is never built, so the query is invalid as a whole
        act.Should().Throw<HLinqQueryException>();
    }

    private static IReadOnlyList<HLinqError> ValidateWith(HLinqOptions options, string query)
    {
        var services = new ServiceCollection();
        HLinqCore.ConfigureServices(services, options);
        services.AddSingleton<IHLinqOptions>(options);
        using var provider = services.BuildServiceProvider();

        return HLinqQuery<DummyEntity>.Validate(provider.GetRequiredService<HLinqBinderDependenciesBag>(), query);
    }

    private HLinqQuery<DummyEntity> Bind(string query)
    {
        var hlinqQuery = new HLinqQuery<DummyEntity>
        {
            SourceQueryString = query
        };
        _parser.Parse(hlinqQuery, _tokenizer.Tokenize(query));
        return hlinqQuery;
    }
}
