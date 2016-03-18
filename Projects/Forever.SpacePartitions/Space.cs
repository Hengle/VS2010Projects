﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Forever.SpacePartitions
{
    /// <summary>
    /// A spatial partitioned matrix of objects. 
    /// </summary>
    public class Space<T> where T: class
    {
        /// <summary>
        /// 3D matrix of cell spaces
        /// </summary>
        Dictionary<SpaceCoord, IPartition<T>> TheMatrix { get; set; }
        Dictionary<T, IPartition<T>> PartitionAssignment { get; set; }

        public IEnumerable<SpaceCoord> Coords { get { return TheMatrix.Keys; } }
        public IEnumerable<IPartition<T>> Partitions { get { return TheMatrix.Values; } }

        public delegate void OnRegisterEventHandler(T obj, Vector3 pos);
        public event OnRegisterEventHandler OnRegister;

        public delegate void OnUnRegisterEventHandler(T obj);
        public event OnUnRegisterEventHandler OnUnRegister;

        public int GridSize { get; private set; }

        public int Count { get; private set; }

        public Space(int gridSize)
        {
            GridSize = gridSize;
            TheMatrix = new Dictionary<SpaceCoord, IPartition<T>>();
            PartitionAssignment = new Dictionary<T, IPartition<T>>();
            Count = 0;
        }

        public void Register(T obj, Vector3 position)
        {
            var coord = PositionToCoord(position, GridSize);
            var par = GetOrCreate(coord, GridSize);
            AssignPartition(obj, par);
            Count++;

            if (OnRegister != null) OnRegister.Invoke(obj, position);
        }

        public void AssignPartition(T obj, IPartition<T> par)
        {
            par.Assign(obj);
            PartitionAssignment[obj] = par;
        }

        public void UnRegister(T obj)
        {
            if (PartitionAssignment.ContainsKey(obj))
            {
                IPartition<T> p = PartitionAssignment[obj];
                p.UnAssign(obj);
                PartitionAssignment.Remove(obj);
            }

            if (OnUnRegister != null) OnUnRegister.Invoke(obj);
        }

        public void Update(T obj, Vector3 position)
        {
            //TODO - this hasn't been tested

            var p = PartitionAssignment[obj];
            if (p.Box.Contains(position) == ContainmentType.Disjoint)
            {
                UnRegister(obj);
                Register(obj, position);
            }
        }


        #region Conversions
        private BoundingBox CoordToBoundingBox(SpaceCoord coord, float boxHalfSize)
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
            
        }

        private IEnumerable<SpaceCoord> CoordBox(SpaceCoord c, int coordBoxHalf=1)
        {
            List<SpaceCoord> list = new List<SpaceCoord>();
            for (int x = -coordBoxHalf; x < coordBoxHalf+1; x++)
                for (int y = -coordBoxHalf; y < coordBoxHalf + 1; y++)
                    for (int z = -coordBoxHalf; z < coordBoxHalf + 1; z++)
                    {

                        list.Add(
                            new SpaceCoord { X = c.X + x, Y = c.Y + y, Z = c.Z + z }
                            );
                    }

            return list;
        }

        private IEnumerable<IPartition<T>> CoordBoxPartitions(SpaceCoord c, int coordBoxHalf = 1)
        {
            List<IPartition<T>> parts = new List<IPartition<T>>();
            var coords = CoordBox(c, coordBoxHalf);
            foreach (var coord in coords)
            {
                if (TheMatrix.ContainsKey(coord))
                {
                    parts.Add(TheMatrix[coord]);
                }
            }

            return parts;
        }


        public IPartition<T> GetOrCreate(Vector3 pos)
        {
            return GetOrCreate(VectorToCoord(pos, GridSize), GridSize);
        }
        private IPartition<T> GetOrCreate(SpaceCoord coord, float boxHalfSize)
        {
            if (!TheMatrix.ContainsKey(coord))
            {
                var box = CoordToBoundingBox(coord, boxHalfSize);
                var par = CreateNewPartition(box);
                TheMatrix.Add(coord, par);
            }
            return TheMatrix[coord];
        }


        protected virtual IPartition<T> CreateNewPartition(BoundingBox box)
        {
            return new Partition<T>(box);
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

            return RawSpaceCoord(x, y, z, boxHalfSize);

        }


        private SpaceCoord RawSpaceCoord(float x, float y, float z, float boxHalfSize)
        {
            return new SpaceCoord
            {
                X = (int)(x / (boxHalfSize*2)),
                Y = (int)(y / (boxHalfSize*2)),
                Z = (int)(z / (boxHalfSize*2)) 
            };
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

       

        public IEnumerable<T> QueryLocalSpace(Vector3 pos, float radius, Func<SpaceCoord, T, bool> test)
        {
            var list = new List<T>();

            int cellRadius = (int)(radius / GridSize);
            var c = VectorToCoord(pos, GridSize);

            foreach (var coord in CoordBox(c, cellRadius))
            {
                var cleanVect = CoordToVector(coord, GridSize);
                if (TheMatrix.ContainsKey(coord))
                {
                    var par = TheMatrix[coord];
                    var members = par.Objects.ToList();
                    foreach (var mem in members)
                    {
                        if (test(coord, mem))
                        {
                            list.Add(mem);
                        }
                    }
                }
            }
            return list;
        }

        public IEnumerable<IPartition<T>> GetSpacePartitions(Vector3 pos, float radius)
        {
            int cellRadius = (int)(radius / GridSize);
            var c = VectorToCoord(pos, GridSize);
            return CoordBoxPartitions(c, cellRadius);
        }
    }
}