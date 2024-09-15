using System.Diagnostics;

namespace WildcardIntersection;

public static class Patterns
{
    public const char Wildcard = '*';
}

public static class PatternFunctions
{
    // In addition to standard C# syntax (e,g., index and range values), the following notation is
    // used throughout the comments below:
    //
    // The ? denotes 0 or 1 characters, including a wildcard character.
    // Example: a? can expand to a, a*, or a followed by an other character.
    //
    //
    // A string x can be written in fragment form, i.e., x = [x1, x2, ..., xn], where the
    // concatenation of all fragments xi results in x.
    //
    // Example:
    // x = abb*bba = [a, bb, *, bb, a] = [abb, *, bba]
    //
    // Note that the ? placeholder is a valid fragment.
    //
    // Example:
    // Given x = [ab, ?, ba], x can then expand to abba, ab*ba, any single character prefixed
    // by abb and suffixed by bba, e.g., abcba.
    //
    //
    // P(x) = Prefix of x up to but not including its wildcard (if present)
    // S(x) = Suffix of x up to but not including its wildcard (if present)
    //
    // Ex 1:
    // Given x = aa, P(x) = S(x) = aa
    //
    // Ex 2:
    // Given x = a*, P(x) = a, S(x) = string.Empty
    //
    // Ex 3:
    // Given x = *a, P(x) = string.Empty, S(x) = a
    //
    // Ex 4:
    // Given x = a*b, P(x) = a, S(x) = b

    public static string IntersectPatterns(ReadOnlySpan<char> x, ReadOnlySpan<char> y)
    {
        int i = 0;

        while (true)
        {
            // Up to this point, x and y have a common, non-wildcard prefix up to but excluding
            // index i, i.e., x[..i] == y[..i].
            Debug.Assert(x[..i].Equals(y[..i], StringComparison.Ordinal));

            if (i == x.Length || i == y.Length)
            {
                // Reached the end of at least one of the patterns.

                if (x.Length == y.Length)
                {
                    // Both patterns are equal. Return either.
                    Debug.Assert(x.Equals(y, StringComparison.Ordinal));
                    return x.ToString();
                }

                if ((i + 1) == y.Length && y[i] == Patterns.Wildcard)
                {
                    // y = x*, thus the intersection is just x.
                    Debug.Assert($"{x}*" == y.ToString());
                    return x.ToString();
                }

                if ((i + 1) == x.Length && x[i] == Patterns.Wildcard)
                {
                    // x = y*, thus the intersection is just y.
                    Debug.Assert($"{y}*" == x.ToString());
                    return y.ToString();
                }

                // One of the patterns has a suffix that can't be matched by the other, so their
                // intersection is empty.
                return string.Empty;
            }

            // If either of the next two cases are true, we have found the longest, non-wildcard
            // prefix between x and y in the range [0..i]. Transition to performing an intersection
            // on the suffixes of x and y.
            if (x[i] == Patterns.Wildcard)
                return IntersectPatternSuffixes(x, y, i);

            if (y[i] == Patterns.Wildcard)
                return IntersectPatternSuffixes(y, x, i);

            if (x[i] != y[i])
            {
                // x and y have non-overlapping prefixes, so their intersection is empty.
                return string.Empty;
            }

            ++i;
        }
    }

    // NOTE: x is a pattern with a wildcard at index iWild. x and y have a common, non-wildcard
    // prefix up to but excluding iWild, i.e., x[..iWild] == y[iWild]
    private static string IntersectPatternSuffixes(
        ReadOnlySpan<char> x,
        ReadOnlySpan<char> y,
        int iWild
    )
    {
        Debug.Assert(x[iWild] == Patterns.Wildcard);
        Debug.Assert(x[..iWild].Equals(y[..iWild], StringComparison.Ordinal));

        int iX = x.Length - 1;
        int iY = y.Length - 1;

        while (true)
        {
            // Up to this point, x and y have a common, non-wildcard suffix from [(iX + 1)..] and
            // [(iY + 1)..], respectively.
            Debug.Assert(x[(iX + 1)..].Equals(y[(iY + 1)..], StringComparison.Ordinal));

            if (iX == iWild)
            {
                // We found the wildcard originally found in x. Any characters in y[iWild..(iY+1)]
                // can be matched by this wildcard in x. Thus, y matches a subset of x, and the
                // intersection of x and y is y.
                //
                // NOTE: We want to ensure that y is well-formed, i.e., it has at most one wildcard.
                // It's possible that y[iWild..(iY + 1)] violates this, so we quickly scan it to
                // ensure its validity before returning.
                if (IsValidPattern(y[iWild..(iY + 1)]))
                    return y.ToString();

                throw new ArgumentException($"Pattern {y} contains multiple wildcards", nameof(y));
            }

            if (x[iX] == Patterns.Wildcard)
            {
                // x has another wildcard beyond iWild, thus it's an invalid pattern.
                Debug.Assert(x.Count(Patterns.Wildcard) > 1);
                throw new ArgumentException($"Pattern {x} contains multiple wildcards", nameof(x));
            }

            if (iY == (iWild - 1))
            {
                // y is an exact match pattern, i.e., it has no wildcards. Furthermore, since at
                // this point iX > iWild and x[(iX + 1)..] == y[iWild..], we know x has characters
                // in the range [(iWild + 1) .. (iX + 1)] that cannot be matched by y because these
                // characters, by definition, cannot be wildcards since x has a wildcard already at
                // iWild. As an example, consider the following patterns:
                //
                // x: aa*aa
                // y: aaa
                //
                // x must match a fourth 'a' character that can never be matched by y. Therefore,
                // in general, at this point we can determine that the intersection will be empty.
                return string.Empty;
            }

            if (y[iY] == Patterns.Wildcard)
            {
                // At this point x and y have matching prefixes given by P(X). We know also have
                // found that x and y have matching suffixes given by S(Y). Given this, we can
                // write the following:
                //
                // x = [P(X), *, Q, S(Y)]
                // y = [PX(X), R, *, S(Y)]
                //
                // Here Q and R and each possibly empty substrings that, by definition, should not
                // contain any wildcards. At this point, we need to find the intersection of *Q and
                // R*, which involves performing a "wildcard compression" on Q and R.
                //
                // RECALL: we need to include the wildcard in the span passed for R.
                return IntersectWildcardSegments(
                    prefix: x[..iWild],
                    suffix: y[(iY + 1)..],
                    q: x[(iWild + 1)..(iX + 1)],
                    rStar: y[iWild..(iY + 1)]
                );
            }

            if (x[iX] != y[iY])
            {
                // x and y have non-overlapping suffixes, so their intersection is empty.
                return string.Empty;
            }

            --iX;
            --iY;
        }
    }

    // The values p, s, q, and r come originally from patterns x and y such that
    //
    // x = [p, *, q, s]
    // y = [p, r, *, s]
    //
    // This function returns the intersection of x and y. To do so, it does a compression of *q and
    // r* such that the resulting wildcard string represents the intersection of *q and r*. This is
    // achieved by trimming the longest prefix of q that is also a suffix of r. Let t be the
    // remaining suffix of q after trimming. The intersection of *q and r* is thus [r, *, t], and
    // the resulting intersection of x and y is [p, r, *, t, s].
    //
    // NOTE: rStar is expected to be the concatenation of [r, *]. This is so that we can optimally
    // join the following four spans using .NET's string.Concat when it's computed: [p, rStar, t, s]
    private static string IntersectWildcardSegments(
        ReadOnlySpan<char> prefix,
        ReadOnlySpan<char> suffix,
        ReadOnlySpan<char> q,
        ReadOnlySpan<char> rStar
    )
    {
        ReadOnlySpan<char> r = rStar[..^1];

        // Align q and r such that we try the largest possible overlap first and then progressively
        // try smaller ones if need be. This ensures we get the maximal prefix of q that is also a
        // suffix of r. Note that if length(q) > length(r), we need to shift where we start scanning
        // so that q[0] is compared with an element of r.
        int iTrim = q.Length;
        if (q.Length > r.Length)
            iTrim -= q.Length - r.Length;

        while (iTrim > 0)
        {
            for (int iQ = iTrim - 1, iR = r.Length - 1; iQ >= 0; --iQ, --iR)
            {
                if (q[iQ] != r[iR])
                    goto UpdateTrim;
            }

            // If we're here, q[..iTrim] is the longest prefix of q that is a suffix of r, so we
            // can return our intersection using our trimmed suffix of q.
            return string.Concat(prefix, rStar, q[iTrim..], suffix);

            UpdateTrim:
            --iTrim;
        }

        // No prefix of q overlapped with a suffix of r, so we simply return [p, rStar, q, s].
        return string.Concat(prefix, rStar, q, suffix);
    }

    private static bool IsValidPattern(ReadOnlySpan<char> x)
    {
        int count = 0;
        foreach (char c in x)
        {
            if (c == Patterns.Wildcard)
            {
                ++count;
                if (count > 1)
                    return false;
            }
        }
        return true;
    }
}
