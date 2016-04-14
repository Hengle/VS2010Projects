﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Forever.Voxel.SVO
{
    public static class NodePosition
    {
        public static readonly int NEAR_BOTTOM_LEFT = 1;
        public static readonly int NEAR_BOTTOM_RIGHT = 5;
        public static readonly int NEAR_TOP_LEFT = 3;
        public static readonly int NEAR_TOP_RIGHT = 7;

        public static readonly int FAR_BOTTOM_LEFT = 0;
        public static readonly int FAR_BOTTOM_RIGHT = 4;
        public static readonly int FAR_TOP_RIGHT = 6;
        public static readonly int FAR_TOP_LEFT = 2;
        //any defined node should be < 8 && > 0
        public static readonly int MaxNodeCount = 8;
    };

}
