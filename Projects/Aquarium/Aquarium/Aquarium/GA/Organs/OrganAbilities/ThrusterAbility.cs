﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aquarium.GA.Signals;
using Aquarium.GA.Bodies;
using Microsoft.Xna.Framework;

namespace Aquarium.GA.Organs.OrganAbilities
{
    public class ThrusterAbility : OrganAbility
    {

        public override int NumInputs
        {
            get { return 1; }
        }

        public override int NumOutputs
        {
            get { return 1; }
        }

        public ThrusterAbility(int param0)
            : base(param0)
        {
            SocketId = param0;
        }

        int SocketId { get; set; }


        public override Signal Fire(NervousSystem nervousSystem, Organ parent, Signal signal)
        {
            var num = signal.Value[0];
            var result = 0;
            if (num > 0.5)
            {
                var rigidBody = nervousSystem.Organism.RigidBody;
                var socket = Fuzzy.CircleIndex(parent.Part.Sockets, SocketId);

                var dir = socket.Normal;
                dir = Vector3.Transform(dir, rigidBody.Orientation);

                var mag = 0.0001f * nervousSystem.Organism.RigidBody.Mass;
                var veloCap = 0.005f;
                var bodyPressurePoint = Vector3.Transform(parent.Part.LocalPosition + socket.LocalPosition, rigidBody.World);


                if (rigidBody.Velocity.Length() < veloCap)
                {
                    rigidBody.addForce(dir * mag, bodyPressurePoint);
                    result = 1;
                }
            }

            return new Signal(new List<double> { result });
        }
    }
}
