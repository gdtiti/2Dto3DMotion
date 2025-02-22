﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Winterdust;

public class BvhOutput : MonoBehaviour
{

    public Transform t_camera;
    private Vector3 position;
    BVH bvh;
    GameObject skeleton = null;

    private int currentFrame;
    public int CurrentFrame
    {
        get => currentFrame;
        set
        {
            currentFrame = value;
            if (skeleton != null && CurrentFrame < numOfFrames)
            {
                bvh.moveSkeleton(skeleton, CurrentFrame);
            }
        }
    }

    public BVH Bvh
    {
        get => bvh;

        set
        {
            bvh = value;
            position = t_camera.position;                                                            // Set the position of bvh.
            skeleton.transform.position = new Vector3(t_camera.position.x, t_camera.position.y, t_camera.position.z + 35f);     // Translate the skeleton, so it is visible from camera.
            Debug.Log("Bvh length:" + bvh.frameCount);
        }
    }

    int numOfFrames = 0;
    //public ControlFrames_viaVideo estimationScript;


    void Start()
    {

        initialiseBVH(Base.base_CurrentDir);        // Initialise BVH instance.

    }

    private void Update()
    {
        //CurrentFrame = estimationScript.currentFrame + 1; // Plus one, because T-Pose is at position 0.

        // Set frame to skeleton
        if (skeleton != null && CurrentFrame < numOfFrames)
        {
            bvh.moveSkeleton(skeleton, CurrentFrame);
            // Debug root rotation
            //Quaternion qrot = bvh.allBones[0].localFrameRotations[currentFrame];
            /*
            Vector3 rotationFromBvh = qrot.eulerAngles;
            // [INITIALIZE]: Convert Euler to Quaternion for each axis. 
            Quaternion qX = Quaternion.AngleAxis(rotationFromBvh.x, Vector3.right);
            Quaternion qY = Quaternion.AngleAxis(rotationFromBvh.y, Vector3.up);
            Quaternion qZ = Quaternion.AngleAxis(rotationFromBvh.z, Vector3.forward);
            Quaternion qrotFinal = qY * qX * qZ; // Multiply them in the rotation order, in case of ZXY.
            */
            //model.rotation = qrot;
        }
    }

    public void initialiseBVH(string dir)
    {
        /* Get the list of files in the directory. */
        if (dir == null)
        {
            Debug.Log("Invalid directory! Can not find bvh file.");
            return;
        }
        Debug.Log("Directory: " + dir);
        string[] fileEntries = Directory.GetFiles(dir);
        string bvhfilename = null;
        foreach (string fileName in fileEntries)
        {
            Debug.Log("File entry: " + fileName);
            string extension = Path.GetExtension(fileName);
            Debug.Log("File extension: " + extension);
            if (extension.CompareTo(".bvh") == 0)
            {
                bvhfilename = fileName;
            }

        }
        if (bvhfilename == null)
        {
            Debug.Log("Bvh not found in this directory!");
        }
        else
        {
            bvh = new BVH(bvhfilename);
            numOfFrames = bvh.frameCount;
            skeleton = bvh.makeDebugSkeleton(false, "ffffff", 0.5f);

        }
    }
}
