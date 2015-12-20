﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Fusee.Base.Common;
using Fusee.Base.Core;
using Fusee.Engine;
using Fusee.Engine.Common;
using Fusee.Engine.Core;
using Fusee.Math;
using Fusee.Math.Core;
using Fusee.Serialization;

namespace Fusee.Engine.Examples.Simple.Core
{
 
    [FuseeApplication(Name = "Simple Example", Description = "A very simple example.")]
    public class Simple : RenderCanvas
    {
        // angle variables
        private static float _angleHorz = MathHelper.PiOver4, _angleVert, _angleVelHorz, _angleVelVert;

        private const float RotationSpeed = 3f;
        private const float Damping = 0.92f;

        private SceneContainer _rocketScene;
        private SceneRenderer _sceneRenderer;
        
        // Init is called on startup. 
        public override void Init()
        {
            // Set the clear color for the backbuffer to white (100% intentsity in all color channels R, G, B, A).
            RC.ClearColor = new float4(1, 1, 1, 1);

            // Load the rocket model
            var ser = new Serializer();
            _rocketScene = ser.Deserialize(IO.StreamFromFile(@"Assets/RocketModel.fus", FileMode.Open), null, typeof(SceneContainer)) as SceneContainer;

            // Wrap a SceneRenderer around the model.
            _sceneRenderer = new SceneRenderer(_rocketScene, "Assets");
        }

        // RenderAFrame is called once a frame
        public override void RenderAFrame()
        {

            // Clear the backbuffer
            RC.Clear(ClearFlags.Color | ClearFlags.Depth);
      
            /*
            // Mouse movement
            if (Input.Instance.Mouse.LeftButton)
            {
                _angleVelHorz = -RotationSpeed* M.Saturate(Input.Instance.Mouse.XVel * 0.00005f, -0.5f, 0.5f);
                _angleVelVert = -RotationSpeed* M.Saturate(Input.Instance.Mouse.YVel * 0.00005f, -0.5f, 0.5f);
                // Diagnostics.Log($"XVel: {Input.Instance.Mouse.XVel}; YVel: {Input.Instance.Mouse.YVel}");
            }
            else
            {
                var curDamp = (float) System.Math.Exp(-Damping*Time.Instance.DeltaTime);

                _angleVelHorz *= curDamp;
                _angleVelVert *= curDamp;
            }
            */

            var lr = Input.Instance.Keyboard.GetLeftRight();
            _angleVelHorz = -RotationSpeed * lr * (float) Time.Instance.DeltaTime;

            var ud = Input.Instance.Keyboard.GetUpDown();
            _angleVelVert = -RotationSpeed * ud * (float)Time.Instance.DeltaTime;

            Diagnostics.Log($"lr: {lr}; ud: {ud}");



            /*
            // Keyboard movement
            if (Input.Instance.Keyboard.GetKey(KeyCodes.Left))
                _angleVelHorz = RotationSpeed*(float) Time.Instance.DeltaTime;
            if (Input.Instance.Keyboard.IsKeyUp(KeyCodes.Left))
                _angleVelHorz = 0;

            if (Input.Instance.Keyboard.GetKey(KeyCodes.Right))
                _angleVelHorz = -RotationSpeed*(float) Time.Instance.DeltaTime;
            if (Input.Instance.Keyboard.IsKeyUp(KeyCodes.Right))
                _angleVelHorz = 0;

            if (Input.Instance.Keyboard.GetKey(KeyCodes.Up))
                _angleVelVert = RotationSpeed*(float) Time.Instance.DeltaTime;
            if (Input.Instance.Keyboard.IsKeyUp(KeyCodes.Up))
                _angleVelVert = 0;

            if (Input.Instance.Keyboard.GetKey(KeyCodes.Down))
                _angleVelVert = -RotationSpeed*(float) Time.Instance.DeltaTime;
            if (Input.Instance.Keyboard.IsKeyUp(KeyCodes.Down))
                _angleVelVert = 0;
            */

            _angleHorz += _angleVelHorz;
            _angleVert += _angleVelVert;

            // Create the camera matrix and set it as the current ModelView transformation
            var mtxRot = float4x4.CreateRotationX(_angleVert) * float4x4.CreateRotationY(_angleHorz);
            var mtxCam = float4x4.LookAt(0, 20, -600, 0, 150, 0, 0, 1, 0);
            RC.ModelView = mtxCam * mtxRot;

            // Render the scene loaded in Init()
            _sceneRenderer.Render(RC);

            // Swap buffers: Show the contents of the backbuffer (containing the currently rerndered farame) on the front buffer.
            Present();
        }

        // Is called when the window was resized
        public override void Resize()
        {
            // Set the new rendering area to the entire new windows size
            RC.Viewport(0, 0, Width, Height);

            // Create a new projection matrix generating undistorted images on the new aspect ratio.
            var aspectRatio = Width/(float) Height;

            // 0.25*PI Rad -> 45° Opening angle along the vertical direction. Horizontal opening angle is calculated based on the aspect ratio
            // Front clipping happens at 1 (Objects nearer than 1 world unit get clipped)
            // Back clipping happens at 2000 (Anything further away from the camera than 2000 world units gets clipped, polygons will be cut)
            var projection = float4x4.CreatePerspectiveFieldOfView(MathHelper.PiOver4, aspectRatio, 1, 20000);
            RC.Projection = projection;
        }
    }
}