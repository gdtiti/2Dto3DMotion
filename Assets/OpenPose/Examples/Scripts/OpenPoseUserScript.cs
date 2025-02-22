﻿// OpenPose Unity Plugin v1.0.0alpha-1.5.0
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
namespace OpenPose.Example {
    /*
     * User of OPWrapper
     */
    public class OpenPoseUserScript : DataInFrame
    {


        // 3D Estimation
        private static Scenario sc = Base.sc;
        private static List<Cluster> base_clusters = Base.base_clusters;                        // Clustered projections.
        private static List<List<Rotations>> base_rotationFiles = Base.base_rotationFiles;      // Rotations.
        public static Vector3[] estimation_to_debug = null;                                     // To debug 2D figure
        public static Vector3[] rawInputToDebug = null;
        public static FigureIdentifier figureIdentifier = new FigureIdentifier();

        // The 2D human to control
        [SerializeField]
        Transform humanContainer;
        [SerializeField]
        ImageRenderer imageRenderer;
        [SerializeField]
        Text fpsText;
        [SerializeField]
        Text peopleText;

        // Output control
        private OPDatum datum;

        // User settings
        public ProducerType inputType = ProducerType.Webcam;
        public string producerString = "-1";
        public int maxPeople = -1;
        public float renderThreshold = 0.05f;
        public bool
            handEnabled = false,
            faceEnabled = false;
        public Vector2Int
            netResolution = new Vector2Int(-1, 368),
            handResolution = new Vector2Int(368, 368),
            faceResolution = new Vector2Int(368, 368);
        public void SetHandEnabled(bool enabled) { handEnabled = enabled; }
        public void SetFaceEnabled(bool enabled) { faceEnabled = enabled; }
        public void SetRenderThreshold(string s) { float res; if (float.TryParse(s, out res)) { renderThreshold = res; }; }
        public void SetMaxPeople(string s) { int res; if (int.TryParse(s, out res)) { maxPeople = res; }; }
        public void SetPoseResX(string s) { int res; if (int.TryParse(s, out res)) { netResolution.x = res; }; }
        public void SetPoseResY(string s) { int res; if (int.TryParse(s, out res)) { netResolution.y = res; }; }
        public void SetHandResX(string s) { int res; if (int.TryParse(s, out res)) { handResolution.x = res; }; }
        public void SetHandResY(string s) { int res; if (int.TryParse(s, out res)) { handResolution.y = res; }; }
        public void SetFaceResX(string s) { int res; if (int.TryParse(s, out res)) { faceResolution.x = res; }; }
        public void SetFaceResY(string s) { int res; if (int.TryParse(s, out res)) { faceResolution.y = res; }; }

        public void ApplyChanges()
        {
            // Restart OpenPose
            StartCoroutine(UserRebootOpenPoseCoroutine());
        }

        // Bg image
        public bool renderBgImg = false;
        public void ToggleRenderBgImg()
        {
            renderBgImg = !renderBgImg;
            OPWrapper.OPEnableImageOutput(renderBgImg);
            imageRenderer.FadeInOut(renderBgImg);
        }

        // Number of people
        int numberPeople = 0;

        // Frame rate calculation
        [Range(0f, 1f)]
        public float frameRateSmoothRatio = 0.8f;
        private float avgFrameRate = 0f;
        private float avgFrameTime = -1f;
        private float lastFrameTime = -1f;




        private void Start()
        {

            // Register callbacks
            OPWrapper.OPRegisterCallbacks();
            // Enable OpenPose run multi-thread (default true)
            OPWrapper.OPEnableMultiThread(true);
            // Enable OpenPose log to unity (default true)
            OPWrapper.OPEnableDebug(true);
            // Enable OpenPose output to unity (default true)
            OPWrapper.OPEnableOutput(true);
            // Enable receiving image (default false)
            OPWrapper.OPEnableImageOutput(renderBgImg);

            // Configure OpenPose with default value, or using specific configuration for each
            /* OPWrapper.OPConfigureAllInDefault(); */
            UserConfigureOpenPose();


            // Start OpenPose
            OPWrapper.OPRun();

            
        }

        // User can change the settings here
        private void UserConfigureOpenPose()
        {
            OPWrapper.OPConfigurePose(
                /* enable */ true, /* netInputSize */ netResolution, /* outputSize */ null,
                /* keypointScaleMode */ ScaleMode.InputResolution,
                /* gpuNumber */ -1, /* gpuNumberStart */ 0, /* scalesNumber */ 1, /* scaleGap */ 0.3f,
                /* renderMode */ RenderMode.Gpu, /* poseModel */ PoseModel.COCO_18, // << CHANGED
                                                                                    /* blendOriginalFrame */ true, /* alphaKeypoint */ 0.6f, /* alphaHeatMap */ 0.7f,
                /* defaultPartToRender */ 0, /* modelFolder */ null,
                /* heatMapTypes */ HeatMapType.None, /* heatMapScaleMode */ ScaleMode.UnsignedChar,
                /* addPartCandidates */ false, /* renderThreshold */ renderThreshold, /* numberPeopleMax */ maxPeople,
                /* maximizePositives */ false, /* fpsMax fps_max */ -1.0,
                /* protoTxtPath */ "", /* caffeModelPath */ "");

            OPWrapper.OPConfigureHand(
                /* enable */ handEnabled, /* netInputSize */ handResolution,
                /* scalesNumber */ 1, /* scaleRange */ 0.4f, /* tracking */ false,
                /* renderMode */ RenderMode.None,
                /* alphaKeypoint */ 0.6f, /* alphaHeatMap */ 0.7f, /* renderThreshold */ 0.2f);

            OPWrapper.OPConfigureFace(
                /* enable */ faceEnabled, /* netInputSize */ faceResolution, /* renderMode */ RenderMode.None,
                /* alphaKeypoint */ 0.6f, /* alphaHeatMap */ 0.7f, /* renderThreshold */ 0.4f);

            OPWrapper.OPConfigureExtra(
                /* reconstruct3d */ false, /* minViews3d */ -1, /* identification */ false, /* tracking */ -1,
                /* ikThreads */ 0);

            OPWrapper.OPConfigureInput(
                /* producerType */ inputType, /* producerString */ producerString,
                /* frameFirst */ 0, /* frameStep */ 1, /* frameLast */ ulong.MaxValue,
                /* realTimeProcessing */ false, /* frameFlip */ false,
                /* frameRotate */ 0, /* framesRepeat */ false,
                /* cameraResolution */ null, /* cameraParameterPath */ null,
                /* undistortImage */ false, /* numberViews */ -1);

            OPWrapper.OPConfigureOutput(
                /* verbose */ -1.0, /* writeKeypoint */ "", /* writeKeypointFormat */ DataFormat.Yml,
                /* writeJson */ "", /* writeCocoJson */ "", /* writeCocoFootJson */ "", /* writeCocoJsonVariant */ 1,
                /* writeImages */ "", /* writeImagesFormat */ "png", /* writeVideo */ "", /* writeVideoFps */ 30.0,
                /* writeHeatMaps */ "", /* writeHeatMapsFormat */ "png", /* writeVideo3D */ "",
                /* writeVideoAdam */ "", /* writeBvh */ "", /* udpHost */ "", /* udpPort */ "8051");

            OPWrapper.OPConfigureGui(
                /* displayMode */ DisplayMode.NoDisplay, /* guiVerbose */ false, /* fullScreen */ false);
        }

        private IEnumerator UserRebootOpenPoseCoroutine()
        {
            if (OPWrapper.state == OPState.None) yield break;
            // Shutdown if running
            if (OPWrapper.state == OPState.Running)
            {
                OPWrapper.OPShutdown();
            }
            // Wait until fully stopped
            yield return new WaitUntil(() => { return OPWrapper.state == OPState.Ready; });
            // Configure and start
            UserConfigureOpenPose();
            OPWrapper.OPRun();
        }


        public static List<OPFrame> frames = new List<OPFrame>();
        private static int currentframeIndex = 0;
        // HERE
        // This fuction is made by me
        private OPPose getFigureFromData(ref OPDatum datum, int personID, float scoreThres, OPFrame frame, int currentFigureIndex)
        {
            if (datum.poseKeypoints == null || personID >= datum.poseKeypoints.GetSize(0))
            {
                return null;
            }

            // Identification :TODO:
            // long id = datum.poseIds.Get(personID);
            // Debug.Log("The id is:"+id);

            // Pose
            Vector3[] joints = new Vector3[OPPose.KEYPOINTS_NUMBER];
            bool[] available = new bool[OPPose.KEYPOINTS_NUMBER];
            for (int i = 0; i < joints.Length; i++)
            {
                // Compare score
                if (datum.poseKeypoints.Get(personID, i, 2) <= scoreThres)
                {
                    available[i] = false;
                }
                else
                {
                    // Save new positions
                    available[i] = true; 
                    // people x bodyparts x (x,y,score)
                    joints[i] = new Vector3(datum.poseKeypoints.Get(personID, i, 0), datum.poseKeypoints.Get(personID, i, 1), 0f);
                }
            }
            // SET NEW FIGURE!
            rawInputToDebug = new Vector3[14];
            Array.Copy(joints, rawInputToDebug, joints.Length);
            OPPose poseNEW = new OPPose(joints, available, frames, currentframeIndex, figureIdentifier);
            return poseNEW;
        }



     
         
        private void estimateAndSave3D(OPPose figure)
        {
            // STEP_A: Find k-BM.
            sc.algNeighbours.SetNeighbours(figure, sc.k, base_clusters);
            // STEP_B: Find Best 3D.
            figure.Estimation3D = sc.algEstimation.SetEstimation(figure, sc.m, base_rotationFiles);
        }




        private void Update()
        {

            // New data received
            if (OPWrapper.OPGetOutput(out datum))
            {
                
                OPFrame currframe = new OPFrame();  // Create a new Frame. Figures list is initiated.
                frames.Add(currframe);              // Add Frame to the list of frames.
                currentframeIndex++;                // Keep track of the frame id.

                // | | | |  Draw human in screen  | | | | //
                int i = 0;
                foreach (var human in humanContainer.GetComponentsInChildren<HumanController2D>())
                {
                    human.DrawHuman(ref datum, i++, renderThreshold);
                }

                // | | | | Render image in screen | | | | //
                imageRenderer.UpdateImage(datum.cvInputData);

                // | | | | Calculate Number of people | | | | //
                if (datum.poseKeypoints == null || datum.poseKeypoints.Empty()) numberPeople = 0;
                else numberPeople = datum.poseKeypoints.GetSize(0);
                peopleText.text = "People: " + numberPeople;

                // | | | | Calculate framerate | | | | //
                if (lastFrameTime > 0f)
                {
                    if (avgFrameTime < 0f) avgFrameTime = Time.time - lastFrameTime;
                    else
                    {
                        avgFrameTime = Mathf.Lerp(Time.time - lastFrameTime, avgFrameTime, frameRateSmoothRatio);
                        avgFrameRate = 1f / avgFrameTime;
                    }
                }
                lastFrameTime = Time.time;
                fpsText.text = avgFrameRate.ToString("F1") + " FPS";

                // | | |  Get figures from data and estimate 3D.  | | | //

                OPPose[] prevFigures = new OPPose[20];  // Instances to the prev figure of each figure id.
                for(int k=0; k<numberPeople; k++)
                {
                    
                    OPPose pose = getFigureFromData(ref datum, k, renderThreshold, currframe, k);  // Get Figure k.
                    currframe.figures.Add(pose);                                                // Add Figure to the Frame. It's ok if it is null.
                    if (pose == null) continue;                                                 // Don't do an estimation if it is null.
                    pose.prevFigure = prevFigures[k];                                           // Set previous figure to the pose.
                    estimateAndSave3D(pose);                                                    // Estimate and Save Figure's 3D.
                    prevFigures[k] = pose;
                }

                // FINALLY, EXPORT DATA TO OTHER SCRIPTS:
                if (personIndex < currframe.figures.Count)
                    selectedPoseToDebug = currframe.figures[personIndex];
                else
                    selectedPoseToDebug = null;
                // AND
                if (currframe != null)
                    allPoses = currframe.figures;
            }

            
        }


    }
}
