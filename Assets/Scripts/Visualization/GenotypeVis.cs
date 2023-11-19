using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using System;

[System.Serializable]

public class GenotypeVis : GraphView
{
    public CreatureGenotype gt = CreatureGenotype.LoadData("/Users/hannahlila04/Downloads/Creatures/arv-vesperferox.creature", true);

    public GenotypeVis()
    {

        List<SegmentGenotype> segments = gt.segments;

        for (int i = 0; i < segments.Count; i++)
        {
            GenotypeNode node = CreateNode();
            SetNode(node, i);
        }

    }

    public GenotypeNode CreateNode()
    {
        GenotypeNode node = new GenotypeNode();

        AddElement(node);

        return node;
    }

    public void SetNode(GenotypeNode node, int SegmentNo)
    {
        node.SegmentNo = SegmentNo;
    }
}

public class GenotypeNode : Node
{
    public int SegmentNo { get; set; }

    public void Intialize()
    {
        SegmentNo = 0;
    }
}


