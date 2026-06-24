using System;
namespace VS.Helper.Core.Versioning;
public static class VsixVersionService
{
    public static Version Increment(Version v)
        => new(v.Major, v.Minor, v.Build < 0 ? 0 : v.Build, (v.Revision < 0 ? 0 : v.Revision) + 1);
}
