using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Tokens.Filtering;
using HamsterWheel.HLinq.Tokens.Selecting;

namespace HamsterWheel.HLinq.Tree.Selecting;

/// <summary>
/// The names one member list answers under; every member list is read the same way.
/// </summary>
internal static class MemberNameCollision
{
    extension(IEnumerable<ITreeElement> members)
    {
        /// <summary>
        /// Refuse two members answering under one name, from the text alone.
        /// </summary>
        public void RefuseDuplicateSelects(string hLinqQuery)
        {
            var answered = new Dictionary<string, IPropertyAssignment>(StringComparer.OrdinalIgnoreCase);

            foreach (var member in members.OfType<IPropertyAssignment>())
            {
                var name = member.FinalPropertyName(hLinqQuery);
                if (name is null || name.Length == 0)
                {
                    continue;
                }

                if (answered.TryGetValue(name, out var first))
                {
                    throw new MemberNameCollisionException(name, first.GetValue(hLinqQuery),
                        member.GetValue(hLinqQuery));
                }

                answered[name] = member;
            }
        }
    }

    extension(IPropertyAssignment member)
    {
        /// <summary>
        /// The text the query wrote as this member's value, quoted back as it stands.
        /// </summary>
        private string GetValue(string hLinqQuery)
        {
            //a rename names the member instead of spelling what it answers, and the tokens the member
            //itself holds are its separating comma, so neither of them is asked for
            var tokens = member.GetTokensOfSubtree(
                token => token is not Comma and not Assignment,
                child => child is not IInitializerPropertyName).ToList();

            return hLinqQuery[tokens.Min(t => t.Range.Start.Value)..tokens.Max(t => t.Range.End.Value)];
        }
    }
}
