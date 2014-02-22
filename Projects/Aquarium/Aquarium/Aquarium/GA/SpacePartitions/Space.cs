﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Aquarium.GA.SpacePartitions
{
    /// <summary>
    /// A spatial partitioned matrix of objects. 
    /// </summary>
    public class Space<T> where T: class
    {
        /// <summary>
        /// 3D matrix of cell spaces
        /// </summary>
        Dictionary<SpaceCoord, Partition<T>> TheMatrix { get; set; }

        public IEnumerable<SpaceCoord> Coords { get { return TheMatrix.Keys; } }
        public IEnumerable<Partition<T>> Partitions { get { return TheMatrix.Values; } }

        int GridSize = 100;

        public int Count { get; private set; }

        public Space()
        {
            TheMatrix = new Dictionary<SpaceCoord, Partition<T>>();
            Count = 0;
        }

        public void Register(T obj, Vector3 position)
        {
            var coord = PositionToCoord(position, GridSize);
            var par = GetOrCreate(coord, GridSize);
            par.Objects.Add(obj);
            if (par.Box.Contains(position) == ContainmentType.Disjoint)
            {
               // throw new Exception();
            }
            Count++;
        }

        public void UnRegister(T obj)
        {
            Partition<T> p = null;
            foreach (var coord in TheMatrix.Keys)
            {
                foreach (var o in TheMatrix[coord].Objects)
                {
                    if (o == obj)
                    {
                        p = TheMatrix[coord];
                    }
                }
            }

            p.Objects.Remove(obj);
        }

        public void Update(T obj, Vector3 position)
        {
            UnRegister(obj);
            Register(obj, position);
        }


        #region Conversions
        private BoundingBox CoordinateBoundingBox(SpaceCoord coord, float boxHalfSize)
        {
            
                Vector3 center = CoordToVector(coord, boxHalfSize);
                var min = new Vector3(
                    coord.X == 0 ? -boxHalfSize * 2 : center.X - boxHalfSize,
                    coord.Y == 0 ? -boxHalfSize * 2 : center.Y - boxHalfSize,
                    coord.Z == 0 ? -boxHalfSize * 2 : center.Z - boxHalfSize);

                var max = new Vector3(
                    coord.X == 0 ? +boxHalfSize * 2 : center.X + boxHalfSize,
                    coord.Y == 0 ? +boxHalfSize * 2 : center.Y + boxHalfSize,
                    coord.Z == 0 ? +boxHalfSize * 2 : center.Z + boxHalfSize);

                return new BoundingBox(min, max);
            /*
            if (!(coord.X == 0 && coord.Y == 0 && coord.Z == 0))
            {
                Vector3 center = CoordToVector(coord, boxHalfSize);
                var min = new Vector3(center.X - boxHalfSize, center.Y - boxHalfSize, center.Z - boxHalfSize);
                var max = new Vector3(center.X + boxHalfSize, center.Y + boxHalfSize, center.Z + boxHalfSize);

                return new BoundingBox(min, max);
            }
            else
            {
                
                var min = new Vector3(0- boxHalfSize*2, 0 - boxHalfSize*2, 0 - boxHalfSize*2);
                var max = new Vector3(0 + boxHalfSize*2, 0 + boxHalfSize*2, 0 + boxHalfSize*2);
                return new BoundingBox(min, max);
            }
             * */
        }


        private Partition<T> GetOrCreate(SpaceCoord coord, float boxHalfSize)
        {
            if (!TheMatrix.ContainsKey(coord))
            {
                var box = CoordinateBoundingBox(coord, boxHalfSize);

                foreach (var cKey in TheMatrix.Keys)
                {
                    var p = TheMatrix[cKey];
                    var ct = p.Box.Contains(box);
                    if (ct == ContainmentType.Contains)
                    {
                        if (coord.X == 0 || coord.Y == 0 || coord.Z == 0
                            || (cKey.X == 0 || cKey.Y == 0 || cKey.Z == 0))
                        {
                            var biggerBox = p.Box.ExtendToContain(box);
                            p.Box = biggerBox;
                            TheMatrix[coord] = p;
                            return TheMatrix[coord];
                            break;
                        }
                        else
                        {
                            throw new Exception();
                        }
                    }
                }

                var par = new Partition<T>(box);
                TheMatrix.Add(coord, par);
            }
            return TheMatrix[coord];
        }

        private SpaceCoord PositionToCoord(Vector3 pos, float boxHalfSize)
        {
            
            foreach (var c in TheMatrix.Keys)
            {
                var p = TheMatrix[c];
                if(p.Box.Contains(pos) != ContainmentType.Disjoint)
                {
                    return c;
                }
            }
            

            return VectorToCoord(pos, boxHalfSize);
        }

        Vector3 CoordToVector(SpaceCoord coord, float boxHalfSize)
        {

            var x = coord.X;
            var y = coord.Y;
            var z = coord.Z;
            var corner =  new Vector3(x, y, z) * (boxHalfSize*2);
            return corner +(new Vector3(1, 1, 1) * boxHalfSize);

        }

        SpaceCoord VectorToCoord(Vector3 vect, float boxHalfSize)
        {
            var x = vect.X;
            var y = vect.Y;
            var z = vect.Z;

            if (x < 0) x -= boxHalfSize*2;
            if (y < 0) y -= boxHalfSize*2;
            if (z < 0) z -= boxHalfSize*2;

            var c = new SpaceCoord
            {
                X = (int)(x / (boxHalfSize*2)),
                Y = (int)(y / (boxHalfSize*2)),
                Z = (int)(z / (boxHalfSize*2)) 
            };
            
            
            return c;

        }
        #endregion


        public IEnumerable<T> Query(Func<SpaceCoord, T,  bool> test)
        {
            var list = new List<T>();

            foreach (var coord in TheMatrix.Keys)
            {
                var par = TheMatrix[coord];
                foreach (var mem in par.Objects)
                {
                    if (test(coord, mem))
                    {
                        list.Add(mem);
                    }
                }
            }
            return list;
        }
    }
}