using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Romi.PathTools
{
    public enum TangentType
    {
        Aligned,
        Free
    }

    public enum SelectedNode
    {
        Main,
        LeftTangent,
        RightTangent
    }

    public enum LoopMode
    {
        Stop,
        Loop,
        PingPong
    }
}