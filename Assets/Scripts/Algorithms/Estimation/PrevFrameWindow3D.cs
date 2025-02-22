﻿
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Winterdust;
using System;
using System.IO;
using System.Globalization;
using UnityEngine.Assertions;

[System.Serializable()]
public class PrevFrameWindow3D : AlgorithmEstimation
{


    public override Neighbour SetEstimation(OPPose current, int m=0, List<List<Rotations>> rotationFiles=null)
    {

        // Case 1: Zero neighbours found for this op pose.
        if (current.neighbours.Count == 0)
            return null;


        // Case 2: Previous is null.
        OPPose previous = current.prevFigure;
        if (previous == null)
        {
            Debug.Log("Previous is null, Returning the first nearest projection.");
            return current.neighbours[0];
        }

        // Case 3: Previous selectedN is not null.
        // Create the window of previous.
        List<List<Vector3>> prevWindow = createPrevWindow(current,m,rotationFiles);

        float min = float.MaxValue;
        Neighbour minNeighbour = null;
        foreach(Neighbour n in current.neighbours)
        {
            // We should skip the same estimation. (?).. <<<<<<<<<
            //if (n.projection == previous.Estimation3D.projection)
            //{
            //    Debug.Log("Excluding estimation of prev frame.");
            //    continue;
            //}
                

            //Debug.Log("Now, lets create the windows.");
            // Create the window of n
            List<List<Vector3>> window = createWindow(n, rotationFiles,n.projection, m);
            float distance = distanceOfWindows(prevWindow,window);
            if (distance == 0)
            {
                continue; // << Which means every prev figure is null.
            }
                // Save the minimum distance.
            if (distance < min)
            {
                min = distance;
                minNeighbour = n;
                minNeighbour.distance3D = distance; // assign the dist.
            }
        }

        //Debug.Log("MIN DISTANCE:" + min);

        if (min == float.MaxValue)          // Means that all prev figures were null
            return current.neighbours[0];   // then return the first one.

        
        return minNeighbour;
    }

    private List<List<Vector3>> createPrevWindow(OPPose current, int m, List<List<Rotations>> rotationFiles)
    {
        List<List<Vector3>> window = new List<List<Vector3>>();
        List<OPPose> prevPoses = new List<OPPose>();
        OPPose previous = current.prevFigure;
        for(int i=0; i<m; i++)
        {
            prevPoses.Add(previous);
            if (previous == null)
                continue;
            previous = previous.prevFigure;
        }


        // So, now we have: |t-1|t-2|t-3|t-4|...|t-n|
        // Create windows
        foreach (OPPose prev in prevPoses)
        {
            if (prev == null || prev.Estimation3D==null || prev.Estimation3D.projection==null)
            {
                window.Add(null);
            }else
            {
                int fileID = prev.Estimation3D.projection.rotationFileID;
                int frameIndex = prev.Estimation3D.projection.frameNum;
                window.Add(rotationFiles[fileID][frameIndex].getComparableRotations());
            }
        }

        //Debug.Log("Creating prev window DONE. Succeeded in "+prevPoses.Count+ " not null poses. But final window list has length of "+window.Count);
       return window;
    }


    private float distanceOfWindows(List<List<Vector3>> w1_prevFrame, List<List<Vector3>> w2)
    {
        float dist = 0f;
        int comparisonCounter = 0;
        // Assure windows have always the same length
        Assert.IsTrue(w1_prevFrame.Count == w2.Count);
        for (int i = 0; i < w1_prevFrame.Count && i < w2.Count; i++)
        {
            // If any of the window spot is null, then we can not compare that spot.
            if (w1_prevFrame[i] == null || w2[i] == null)
                continue;

            // For each joint, get the distance
            comparisonCounter++;
            for (int j = 0; j < w1_prevFrame[i].Count; j++)
            {
                dist += DistanceRotations(w1_prevFrame[i][j], w2[i][j]);
            }
        }
        float result = dist / comparisonCounter;
        return result; // return average distance.
    }

    private static string disp(Quaternion q)
    {
        return q.x + " " + q.y + " " + q.z + " " + q.w;
    }

    public static float DistanceRotations(Vector3 r1, Vector3 r2)
    {
        Quaternion qA = Quaternion.Euler(r1);
        Quaternion qB = Quaternion.Euler(r2);
        float normqA = norm(qA);
        float normqB = norm(qB);
        Quaternion qA_norm = new Quaternion( qA.x / normqA, qA.y / normqA, qA.z / normqA, qA.w / normqA);
        Quaternion qB_norm = new Quaternion( qB.x / normqB, qB.y / normqB, qB.z / normqB, qB.w / normqB);
        Quaternion quatinv = Quaternion.Inverse(qA_norm); // is this correct?
        Quaternion multiplication = qB_norm*quatinv;
        Quaternion qlog = quatlog(multiplication); 
        float qLogNorm = norm(qlog);
        float result = Mathf.Pow(qLogNorm, 2);

        /*
        // Debug calculations
        string s = "";
        s += "EulA\n" + r1.x + " " + r1.y + " " + r1.z + "\n";
        s += "EulB\n" + r2.x + " " + r2.y + " " + r2.z + "\n";
        s += "QuatA\n " + disp(qA) + "\n";
        s += "QuatB\n " + disp(qB) + "\n";
        s += "QuatA.norm\n " + disp(qA) + "\n";
        s += "QuatB.norm\n " + disp(qB) + "\n";
        s += "Quatinv\n " + disp(quatinv) + "\n";
        s += "QuatMultiplication\n " + disp(multiplication) + "\n";
        s += "QuatMultiplication.Real\n not applied \n";
        s += "QuatLog\n " + disp(qlog) + "\n";
        s += "QuatLog.Real\n not applied\n";
        s += "QuatLogReal.Norm \n " + qLogNorm + "\n";
        s += " QuatLogRealNormPow2(result) \n " + result + "\n";
        Debug.Log(s);
        */
        if (!HasValue(result))
        {
            return 0;
        }
        return result;
    }



    // Or IsNanOrInfinity
    public static bool HasValue(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }


    private static Quaternion quatlog(Quaternion q)
    {
        float alpha = Mathf.Acos(q.w);
        float sina = Mathf.Sin(alpha);
        return new Quaternion(q.x*alpha/sina, q.y*alpha/sina, q.z*alpha/sina,0);
    }

    private static float norm(Quaternion q)
    {
        return Mathf.Sqrt(Mathf.Pow(q.w, 2) + Mathf.Pow(q.x, 2) + Mathf.Pow(q.y, 2) + Mathf.Pow(q.z, 2));
    }


    private List<List<Vector3>> createWindow(Neighbour n, List<List<Rotations>> rotationFiles,BvhProjection projection, int m)
    {
        List<List<Vector3>> window = new List<List<Vector3>>();
        int mainFrameNum = projection.frameNum;
        bool alreadySavedWindows = false;
        if (n.windowIn3Dpoints.Count != 0) 
            alreadySavedWindows = true;

        for (int i=1; i<=m; i++) // Includes the corresponding 3D, of the selected projection.
        {
            // Check if that frame exists in the rotationFile.
            if (mainFrameNum - i < 0)
                window.Add(null);
            else
            {
                int frameIndex = mainFrameNum - i;
                window.Add(rotationFiles[projection.rotationFileID][frameIndex].getComparableRotations());
                // Save the window : each figure as 3D points (so we can debug it later).
                // So.. the problem here is that if Neighbour n, already has a window saved, we shouldn't save it again.
                if(!alreadySavedWindows)
                  save3DpointsInWindow(n, projection.rotationFileID, frameIndex, Base.projectionsPerFrame);
            }
            
        }
        //Debug.Log("Created window for "+"["+n.projection.clusterID+","+ n.projection.frameNum+"]");
        //Debug.Log("Lengths: "+window[0]?.Count+", "+ window[1]?.Count + ", " + window[2]?.Count + ", " + window[3]?.Count);
        return window;
    }

    private void save3DpointsInWindow(Neighbour n, int fileIndex, int frameIndex, int projectionsPerFrame)
    {
        int rotationDegrees = 360 / projectionsPerFrame;
        n.windowIn3Dpoints.Add(Base.base_not_clustered[fileIndex][frameIndex*projectionsPerFrame + n.projection.angle/rotationDegrees]);
    }

}
