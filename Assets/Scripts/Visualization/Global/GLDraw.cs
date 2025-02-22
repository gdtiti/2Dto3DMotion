﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class GLDraw {

    private int[,] jointPairs;
    private int[,] defaultJP = new int[13,2] { {0,1}, {1,2}, {2,3}, {3,4}, {1,5}, {5,6}, {6,7}, {1,8}, {8,9}, {9,10}, {1,11}, {11,12}, {12,13} };
    private Material material;

    public GLDraw(int [,] joint_pairs, Material mat)
    {
        jointPairs = joint_pairs;
        material = mat;
    }

    public GLDraw(Material mat)
    {
        jointPairs = defaultJP;
        material = mat;
    }

    public void drawHorizontalLine(Color color, float y, float length, float x=0)
    {
        GL.Begin(GL.LINES);
        material.SetPass(0);
        GL.Color(color);
        GL.Vertex(new Vector3(-length/2+x,y));
        GL.Vertex(new Vector3(length/2+x, y));
        GL.End();
    }


    public void drawFigure(bool direction,Color color, Vector3[] joints, bool[] available, Vector3 offset, float scaling = 1f)
    {
        GL.Begin(GL.LINES);
        material.SetPass(0);
        GL.Color(color);
        for (int i=0; i<13; i++)
        {
           
            if( (available == null) || (available!=null && available.Length!=0 && available[jointPairs[i, 0]]==true && available[jointPairs[i, 1]]==true))
            {
                // To tell where the stick figure is looking at
                if (direction && (i==2 || i==3 || i==8 || i==9))
                    GL.Color(Color.magenta);

                GL.Vertex(   (joints[jointPairs[i, 0]]*scaling + offset)  );
                GL.Vertex(   (joints[jointPairs[i, 1]]*scaling + offset)  );
                GL.Color(color);
            }
        }
        GL.End();
    }

    public void drawFigureDebug(bool direction, Color color, Vector3[] joints, bool[] available, Vector3 offset, float scaling = 1f)
    {
        GL.Begin(GL.LINES);
        material.SetPass(0);
        GL.Color(color);
        for (int i = 0; i < 13; i++)
        {

            if ((available == null) || (available != null && available[jointPairs[i, 0]] == true && available[jointPairs[i, 1]] == true))
            {
                // To tell where the stick figure is looking at
                if (direction && (i == 8 || i==11 || i==2 || i==5) )
                    GL.Color(Color.magenta);
                //if (direction && (i == 9 || i==12) )
                //    GL.Color(Color.green);
                if (!direction && (i == 8 || i == 11 || i == 2 || i == 5))
                    GL.Color(Color.cyan);
                //if (!direction && (i == 9 || i == 12))
                //    GL.Color(Color.yellow);


                GL.Vertex( (joints[jointPairs[i, 0]]*scaling + offset) );
                GL.Vertex( (joints[jointPairs[i, 1]]*scaling + offset) );
                GL.Color(color);
            }
        }
        GL.End();
    }



    public void drawAxes(Color color, Vector3 center, float xStart=-1000, float xEnd=1000, float yStart=-1000, float yEnd=1000)
    {
        // X-axis
        GL.Begin(GL.LINES);
        material.SetPass(0);
        GL.Color(color);
        GL.Vertex(new Vector3(xStart, center.y, center.z));
        GL.Vertex(center);
        GL.Vertex(center);
        GL.Vertex(new Vector3(xEnd, center.y, center.z));
        // Y-axes
        GL.Vertex(new Vector3(center.x, yStart, center.z));
        GL.Vertex(center);
        GL.Vertex(center);
        GL.Vertex(new Vector3(center.x, yEnd, center.z));
        GL.End();
    }



    public void drawMotionGraphAllJoints(Neighbour[] path, int jointIndex, int currentFrame)
    {
        // Prepare axes (grid)
        drawAxes(Color.white,Vector3.zero);
        float offset=0;
        for (int i = 0; i < path.Length; i++)
        {
            drawVerticalLine(offset+=0.2f, Color.gray);
        }
        // First 3d, starts at 0.2f
        drawVerticalLine(currentFrame*0.2f + 0.2f, Color.red);

        // Draw motion graph
        drawGraph(jointIndex, 100, path);

    }

    public void drawVerticalLine(float x, Color color)
    {
        GL.Begin(GL.LINES);
        material.SetPass(0);
        GL.Color(color);
        GL.Vertex(new Vector3(x, -1000, 0));
        GL.Vertex(new Vector3(x, 1000, 0));
        GL.End();
    }


    public void drawGraph(int jointIndex, float offset, Neighbour[] path)
    {
        drawMotionGraphJoint(jointIndex, Color.yellow, 'x', path);
        drawMotionGraphJoint(jointIndex, Color.green, 'y', path);
        drawMotionGraphJoint(jointIndex, Color.red, 'z', path);
    }

    public void drawMotionGraphJoint(int jointIndex, Color color, char axis, Neighbour[] path)
    {
        float d = 0.2f;
        GL.Begin(GL.LINE_STRIP);
        material.SetPass(0);
        GL.Color(color);
        float offset = 0;
        float point;
        GL.Vertex(Vector3.zero);

        for (int i = 1; i < path.Length; i++)
        {
            

            if (path != null && path[i] != null && path[i].projection != null)
            {
                switch (axis)
                {
                    case 'x': { point = path[i].projection.joints[jointIndex].x ; break; }
                    case 'y': { point = path[i].projection.joints[jointIndex].y ; break; }
                    case 'z': { point = path[i].projection.joints[jointIndex].z ; break; }
                    default : { point = 0; break; }

                }
                GL.Vertex(new Vector3(offset+=d, point, 0));
            }
        }
        GL.End();
    }

    public void drawCube()
    {

        Vector3[] v = {
            new Vector3 (0, 0, 0),
            new Vector3 (1, 0, 0),
            new Vector3 (1, 1, 0),
            new Vector3 (0, 1, 0),
            new Vector3 (0, 1, 1),
            new Vector3 (1, 1, 1),
            new Vector3 (1, 0, 1),
            new Vector3 (0, 0, 1),
        };



        GL.PushMatrix();
        material.SetPass(1);
        GL.Begin(GL.QUADS);
        square(v[0],v[1],v[2],v[3]);
        square(v[3], v[4], v[5], v[2]);
        square(v[2], v[5], v[6], v[1]);
        square(v[0], v[7], v[6], v[1]);
        square(v[4], v[7], v[5], v[6]);
        square(v[0], v[3], v[4], v[7]);
        GL.End();
        GL.PopMatrix();
    }

    private void square(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
    {
        GL.Vertex(v1);
        GL.Vertex(v2);
        GL.Vertex(v3);
        GL.Vertex(v4);
    }
}
