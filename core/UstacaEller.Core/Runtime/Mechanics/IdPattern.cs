using System;

namespace UstacaEller.Core.Mechanics
{
    /// <summary>
    /// Object id matching for manifest <c>accepts</c> and <c>acceptedBy</c> lists.
    /// A trailing <c>*</c> is a prefix wildcard; anything else is an exact match.
    /// Kept identical to the rule in tools/validate-scenes.mjs — if the two ever
    /// disagree, the validator would bless scenes the runtime cannot play.
    /// </summary>
    public static class IdPattern
    {
        public static bool Matches(string pattern, string id)
        {
            if (string.IsNullOrEmpty(pattern) || id == null) return false;

            if (pattern[pattern.Length - 1] == '*')
            {
                string prefix = pattern.Substring(0, pattern.Length - 1);
                return id.StartsWith(prefix, StringComparison.Ordinal);
            }

            return string.Equals(pattern, id, StringComparison.Ordinal);
        }
    }
}
