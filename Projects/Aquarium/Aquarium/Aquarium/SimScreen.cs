﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using Microsoft.Xna.Framework;
using Forever.Screens;
using Aquarium.GA.Population;
using Forever.Render;
using Forever.Physics;
using Aquarium.GA.SpacePartitions;
using Aquarium.GA.Environments;
using Aquarium.GA.Genomes;
using System.Collections.Concurrent;
using Aquarium.GA;

namespace Aquarium
{


    public class SimScreen : GameScreen
    {
        public RandomPopulation Pop { get; private set; }

        protected RenderContext RenderContext { get; private set; }
        public IRigidBody CamBody { get; private set; }
        public UserControls CamControls { get; private set; }

        private int DrawRadius { get; set; }
        private int UpdateRadius { get; set; }


        public Space<PopulationMember> Coarse { get; private set; }
        public EnvironmentSpace Fine { get; private set; }

        BoundingSphere WarpSphere { get; set; }

        Thread GenerateThread;

        public SimScreen(RenderContext renderContext)
        {

            RenderContext = renderContext;
            Coarse = new Space<PopulationMember>(500);
            Fine = new EnvironmentSpace(500, 200);

            int minPopSize = 150;
            int maxPopSize = 150;
            int spawnRange = 90;
            int geneCap = 2000;

            DrawRadius = 10000;
            UpdateRadius = 5000;

            WarpSphere = new BoundingSphere(Vector3.Zero, 200);

            Pop = new RandomPopulation(minPopSize, maxPopSize, spawnRange, geneCap);
            Pop.OnAdd += new Population.OnAddEventHandler((mem) =>
            {
                Coarse.Register(mem, mem.Position);
                Fine.Register(mem as IEnvMember, mem.Position);
            });

            Pop.OnRemove += new Population.OnRemoveEventHandler((mem) =>
            {

                Coarse.UnRegister(mem);
                Fine.UnRegister(mem as IEnvMember);
            });

            Pop.GenerateUntilSize(minPopSize / 2, Pop.SpawnRange, 10);


            GenerateThread = new Thread(new ThreadStart(
                () => {
                    while (true)
                    {
                        UpdatePopMonitoring();
                        System.Threading.Thread.Sleep(10);
                    }
                    
                }));
        }


        public override void LoadContent()
        {
            base.LoadContent();

            SetupCamera();


            GenerateThread.IsBackground = true;
            GenerateThread.Start();

        }
        public override void UnloadContent()
        {
            GenerateThread.Abort();
            System.Threading.SpinWait.SpinUntil(() => 
                {
                    System.Threading.Thread.Sleep(100);
                    return !GenerateThread.IsAlive;
                }
                );
            base.UnloadContent();

        }

        Partition<PopulationMember> CurrentDrawingPartition { get; set; }
        IEnumerable<Partition<PopulationMember>> CurrentDrawingPartitions { get; set; }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            var context = RenderContext;
            var camPos = context.Camera.Position;

            TrackDrawingPartitions(camPos);

            foreach (var part in CurrentDrawingPartitions)
            {
                var members = part.Objects.ToList();
                foreach (var member in members)
                {
                    member.Specimen.Body.Render(RenderContext);
                }

                if (part.Box.Contains(camPos) != ContainmentType.Disjoint || part.Objects.Any())
                {
                    Renderer.Render(context, part.Box, Color.Red);
                }
            }
 
        }

        private void TrackDrawingPartitions(Vector3 camPos)
        {
            if (CurrentDrawingPartition != null)
            {
                if (CurrentDrawingPartition.Box.Contains(camPos) != ContainmentType.Contains)
                {

                    CurrentDrawingPartition = Coarse.GetOrCreate(camPos);
                    CurrentDrawingPartitions = Coarse.GetSpacePartitions(camPos, DrawRadius);
                }
            }
            else
            {
                CurrentDrawingPartition = Coarse.GetOrCreate(camPos);
                CurrentDrawingPartitions = Coarse.GetSpacePartitions(camPos, DrawRadius);
            }
        }

        public void Death(IEnumerable<PopulationMember> members)
        {
            foreach (var mem in members)
            {
                Pop.UnRegister(mem);
            }
        }




        Partition<IEnvMember> CurrentUpdatingPartition { get; set; }
        IEnumerable<Partition<IEnvMember>> CurrentUpdatingPartitions { get; set; }
        
        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            UpdateCamera(gameTime);
          
            float duration = (float)gameTime.ElapsedGameTime.Milliseconds;

            var camPos = RenderContext.Camera.Position;

            if (CurrentUpdatingPartition != null)
            {
                if (CurrentUpdatingPartition.Box.Contains(camPos) != ContainmentType.Contains)
                {
                    CurrentUpdatingPartition = Fine.GetOrCreate(camPos);
                    CurrentUpdatingPartitions = Fine.GetSpacePartitions(camPos, UpdateRadius);

                }
            }
            else
            {
                CurrentUpdatingPartition = Fine.GetOrCreate(camPos);
                CurrentUpdatingPartitions = Fine.GetSpacePartitions(camPos, UpdateRadius);
            }

            var dead = new List<PopulationMember>();

            foreach (var part in CurrentUpdatingPartitions)
            {
                var members = part.Objects.ToList();

                foreach (var envMember in members)
                {
                    var member = envMember.Member;
                    member.Specimen.Update(duration);
                    member.Specimen.Position =  Fuzzy.SphereWrap(member.Position, WarpSphere);
                    if (member.Specimen.IsDead)
                    {
                        dead.Add(member);
                    }
                    else
                    {
                        var rigidBody = member.Specimen.RigidBody;
                        if (rigidBody.Velocity.LengthSquared() > 0 && rigidBody.Awake)
                        {
                            Coarse.Update(member, member.Position);
                            Fine.Update(member, member.Position);
                        }
                    }
                }
            }


            Death(dead); //death to the dead

            var births = Births.ToList().Take(5);
            foreach(var newBirth in births){
                // new life to the living
                if (newBirth != null)
                {
                    Pop.Register(newBirth);
                    PopulationMember localValue;
                    Births.TryDequeue(out localValue);
                }
            }            

            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
        }

        ConcurrentQueue<PopulationMember> Births = new ConcurrentQueue<PopulationMember>();

        #region Camera Controls

        protected ICamera Camera { get { return RenderContext.Camera; } }
        private void SetupCamera()
        {
            var cam = Camera;
            CamBody = new RigidBody(cam.Position);
            CamBody.Awake = true;
            CamBody.LinearDamping = 0.9f;
            CamBody.AngularDamping = 0.7f;
            CamBody.Mass = 0.1f;
            CamBody.InertiaTensor = InertiaTensorFactory.Sphere(CamBody.Mass, 1f);
            CamControls = new UserControls(PlayerIndex.One, 0.000015f, 0.0025f, 0.0003f, 0.001f);
        }

        protected void UpdateCamera(GameTime gameTime)
        {
            Vector3 actuatorTrans = CamControls.LocalForce;
            Vector3 actuatorRot = CamControls.LocalTorque;


            float forwardForceMag = -actuatorTrans.Z;
            float rightForceMag = actuatorTrans.X;
            float upForceMag = actuatorTrans.Y;

            Vector3 force =
                (Camera.Forward * forwardForceMag) +
                (Camera.Right * rightForceMag) +
                (Camera.Up * upForceMag);


            if (force.Length() != 0)
            {
                CamBody.addForce(force);
            }


            Vector3 worldTorque = Vector3.Transform(CamControls.LocalTorque, CamBody.Orientation);

            if (worldTorque.Length() != 0)
            {
                CamBody.addTorque(worldTorque);
            }
            
            CamBody.integrate((float)gameTime.ElapsedGameTime.Milliseconds);
            Camera.Position = CamBody.Position;
            Camera.Rotation = CamBody.Orientation;
            
        }


        public override void HandleInput(InputState input)
        {
            base.HandleInput(input);
            CamControls.HandleInput(input);
        }
        #endregion


        #region Population Monitoring
        protected void UpdatePopMonitoring()
        {
            var random = new Random();
            var members = Pop.ToList();

            var spawnRange = Pop.SpawnRange;

            if (Pop.Size + Births.Count() > Pop.MaxPop) return;
            Action<BodyGenome> birther = (BodyGenome off) =>
            {

                Pop.Splicer.Mutate(off);

                var spawn = Population.SpawnFromGenome(off);
                if (spawn != null)
                {
                    var r = random.NextVector();

                    r *= spawnRange;
                   
                    spawn.Position = r;

                    var mem = new PopulationMember(off, spawn);
                    Births.Enqueue(mem);
                }
            };

            var p1 = random.NextElement(members);
            var p2 = random.NextElement(members);

            var a1 = p1.Specimen.Age;
            var a2 = p2.Specimen.Age;


            var offspring = Pop.Splicer.Meiosis(p1.Genome, p2.Genome);

            birther(offspring[0]);
            birther(offspring[1]);

            if (a1 > a2)
            {
                p2 = random.NextElement(members);
            }
            else
            {
                p1 = random.NextElement(members);
            }
            offspring = Pop.Splicer.Meiosis(p1.Genome, p2.Genome);

            birther(offspring[0]);
            birther(offspring[1]);


        }
        #endregion
    }
}
