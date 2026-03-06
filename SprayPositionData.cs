using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace OnirismSpraypaint
{
    [Serializable]
    public record SprayPositionData(string Filename, PositionData Position);
}

//fixes a bug with the above constructor.
namespace System.Runtime.CompilerServices
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal class IsExternalInit { }
}