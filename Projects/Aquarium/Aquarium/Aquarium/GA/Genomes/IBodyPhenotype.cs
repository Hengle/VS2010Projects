﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Aquarium.GA.Genomes
{
    public interface IBodyPhenotype
    {
        int NumBodyParts { get; }
        List<IBodyPartPhenotype> BodyPartGenomes { get; set; }

    }

    public interface IBodyPartPhenotype
    {
        int BodyPartGeometryIndex { get; set; }
        Color Color { get; set; }

        IInstancePointer AnchorPart { get; set; }
        IInstancePointer PlacementPartSocket { get; set; }

        List<IOrganGenome> OrganGenomes { get; set; }
        List<IBodyPartSocketGenome> SocketGenomes { get; set; }
        IChanneledSignalGenome ChanneledSignalGenome { get; set; }

        Vector3 Scale { get; set; }

    }

    public interface IChanneledSignalGenome : IInstancePointer { }

    public interface IBodyPartSocketGenome : IInstancePointer
    {
        IForeignBodyPartSocketGenome ForeignSocket { get; set; }
    }
    public interface IForeignBodyPartSocketGenome : IInstancePointer
    {
        IInstancePointer BodyPart { get; set; }
    }

    public interface IInstancePointer
    {
        int InstanceId { get; set; }
    }


    public interface IOrganGenome
    {

        IInstancePointer BodyPointer { get; set; }

        IOrganAbilityGenome OrganAbilityGenome { get;  }
    }

    public interface IOrganAbilityGenome
    {

    }

    public interface INeuralNetworkGenome
    {
        int NumHidden { get; set; }

        int NumInputs { get; set; }

        int NumOutputs { get; set; }

        double[] Weights { get; set; }
    }

}
