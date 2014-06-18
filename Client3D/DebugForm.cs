﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Client3D
{
	internal partial class DebugForm : Form
	{
		TerrainRenderer m_scene;
		Timer m_timer;

		public DebugForm()
		{
			InitializeComponent();

			m_timer = new Timer();
			m_timer.Tick += timer_Tick;
			m_timer.Interval = 1000;
			m_timer.Start();
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			m_timer.Stop();

			base.OnClosing(e);
		}

		public void SetScene(TerrainRenderer scene)
		{
			m_scene = scene;

			this.viewCorner1TextBox.Text = scene.ViewCorner1.ToString();
			this.viewCorner2TextBox.Text = scene.ViewCorner2.ToString();

			this.zCutTrackBar.Maximum = scene.Map.Depth - 1;
			this.zCutTrackBar.Value = scene.ViewCorner2.Z;
			this.zCutTrackBar.ValueChanged += (s, e) =>
			{
				scene.ViewCorner2 = scene.ViewCorner2.SetZ(this.zCutTrackBar.Value);
				this.viewCorner2TextBox.Text = scene.ViewCorner2.ToString();
			};

			this.xnCutTrackBar.Maximum = scene.Map.Width - 1;
			this.xnCutTrackBar.Value = scene.ViewCorner1.X;
			this.xnCutTrackBar.ValueChanged += (s, e) =>
			{
				scene.ViewCorner1 = scene.ViewCorner1.SetX(this.xnCutTrackBar.Value);
				this.xnCutTrackBar.Value = scene.ViewCorner1.X;
				this.viewCorner1TextBox.Text = scene.ViewCorner1.ToString();
			};

			this.xpCutTrackBar.Maximum = scene.Map.Width - 1;
			this.xpCutTrackBar.Value = scene.ViewCorner2.X;
			this.xpCutTrackBar.ValueChanged += (s, e) =>
			{
				scene.ViewCorner2 = scene.ViewCorner2.SetX(this.xpCutTrackBar.Value);
				this.xpCutTrackBar.Value = scene.ViewCorner2.X;
				this.viewCorner2TextBox.Text = scene.ViewCorner2.ToString();
			};

			this.ynCutTrackBar.Maximum = scene.Map.Width - 1;
			this.ynCutTrackBar.Value = scene.ViewCorner1.Y;
			this.ynCutTrackBar.ValueChanged += (s, e) =>
			{
				scene.ViewCorner1 = scene.ViewCorner1.SetY(this.ynCutTrackBar.Value);
				this.ynCutTrackBar.Value = scene.ViewCorner1.Y;
				this.viewCorner1TextBox.Text = scene.ViewCorner1.ToString();
			};

			this.ypCutTrackBar.Maximum = scene.Map.Width - 1;
			this.ypCutTrackBar.Value = scene.ViewCorner2.Y;
			this.ypCutTrackBar.ValueChanged += (s, e) =>
			{
				scene.ViewCorner2 = scene.ViewCorner2.SetY(this.ypCutTrackBar.Value);
				this.ypCutTrackBar.Value = scene.ViewCorner2.Y;
				this.viewCorner2TextBox.Text = scene.ViewCorner2.ToString();
			};

			this.checkBox1.CheckedChanged += checkBox_CheckedChanged;
			this.checkBox2.CheckedChanged += checkBox_CheckedChanged;

			this.checkBox3.CheckedChanged += (s, e) => m_scene.Effect.Parameters["g_showBorders"].SetValue(checkBox3.Checked);
			this.checkBox4.CheckedChanged += (s, e) => m_scene.Effect.Parameters["g_disableLight"].SetValue(checkBox4.Checked);
		}

		void checkBox_CheckedChanged(object sender, EventArgs e)
		{
			bool disableCull = checkBox1.Checked;
			bool wire = checkBox2.Checked;

			SharpDX.Toolkit.Graphics.RasterizerState state;

			if (!disableCull && !wire)
				state = m_scene.Game.GraphicsDevice.RasterizerStates.CullBack;
			else if (disableCull && !wire)
				state = m_scene.Game.GraphicsDevice.RasterizerStates.CullNone;
			else if (!disableCull && wire)
				state = m_scene.Game.GraphicsDevice.RasterizerStates.WireFrame;
			else if (disableCull && wire)
				state = m_scene.Game.GraphicsDevice.RasterizerStates.WireFrameCullNone;
			else
				throw new Exception();

			m_scene.RasterizerState = state;
		}

		void timer_Tick(object sender, EventArgs e)
		{
			var cam = m_scene.Services.GetService<ICameraService>();

			this.camPosTextBox.Text = String.Format("{0:F2}/{1:F2}/{2:F2}",
				cam.Position.X, cam.Position.Y, cam.Position.Z);
			this.vertRendTextBox.Text = m_scene.VerticesRendered.ToString();
			this.chunkRecalcsTextBox.Text = m_scene.ChunkRecalcs.ToString();
		}
	}
}
