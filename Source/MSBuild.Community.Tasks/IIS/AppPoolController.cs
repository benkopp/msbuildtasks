#region Copyright � 2005 Paul Welter. All rights reserved.
/*
Copyright � 2005 Paul Welter. All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions
are met:

1. Redistributions of source code must retain the above copyright
   notice, this list of conditions and the following disclaimer.
2. Redistributions in binary form must reproduce the above copyright
   notice, this list of conditions and the following disclaimer in the
   documentation and/or other materials provided with the distribution.
3. The name of the author may not be used to endorse or promote products
   derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE AUTHOR "AS IS" AND ANY EXPRESS OR
IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.
IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY DIRECT, INDIRECT,
INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF
THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE. 
*/
#endregion

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;
using System.Management;

namespace MSBuild.Community.Tasks.IIS
{
	/// <summary>
	/// Allows control for an application pool on a local or remote machine with IIS installed.  The default is 
	/// to control the application pool on the local machine.  If connecting to a remote machine, you can
	/// specify the <see cref="WebBase.Username"/> and <see cref="WebBase.Password"/> for the task
	/// to run under.
	/// </summary>
	/// <example>Restart an application pool on the local machine.
	/// <code><![CDATA[
	/// <AppPoolController AppPoolName="MyAppPool" Action="Restart" />
	/// ]]></code>
	/// </example>
    public class AppPoolController : WebBase
	{
		#region Fields

		private string mAppPoolName;
		private ApplicationPoolAction mAction;

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets the name of the app pool.
		/// </summary>
		/// <value>The name of the app pool.</value>
		[Required]
		public string ApplicationPoolName
		{
			get
			{
				return mAppPoolName;
			}
			set
			{
				mAppPoolName = value;
			}
		}

		/// <summary>
		/// Gets or sets the application pool action.
		/// </summary>
		/// <value>The application pool action.</value>
		[Required]
		public ApplicationPoolAction Action
		{
			get
			{
				return mAction;
			}
			set
			{
				mAction = value;
			}
		}

		#endregion

		/// <summary>
		/// When overridden in a derived class, executes the task.
		/// </summary>
		/// <returns>
		/// True if the task successfully executed; otherwise, false.
		/// </returns>
        public override bool Execute()
        {
			mIISVersion = GetIISVersion();

			if (ControlApplicationPool())
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		#region Private Methods

		private bool ControlApplicationPool()
		{
			bool bSuccess = false;

			try
			{
				VerifyIISRoot();

				if (mIISVersion == IISVersion.Six)
				{
					ConnectionOptions options = new ConnectionOptions();
					if (Username != null && Password != null)
					{
						options.Username = Username;
						options.Password = Password;
					}
					options.Authentication = AuthenticationLevel.PacketPrivacy;
					ManagementScope scope = new ManagementScope(@"\\" + ServerName + "\\root\\MicrosoftIISv2", options);
					scope.Connect();
					ManagementPath path = new ManagementPath("IIsApplicationPool='W3SVC/AppPools/" + ApplicationPoolName + "'");
					ManagementObject pool = new ManagementObject(scope, path, null);

					if (Action == ApplicationPoolAction.Stop || Action == ApplicationPoolAction.Restart)
					{
						Log.LogMessage(MessageImportance.Normal, "Stopping \"{0}\" on \"{1}\"...", ApplicationPoolName, ServerName);
						pool.InvokeMethod("Stop", new object[0]);
					}

					if (Action == ApplicationPoolAction.Start || Action == ApplicationPoolAction.Restart)
					{
						Log.LogMessage(MessageImportance.Normal, "Starting \"{0}\" on \"{1}\"...", ApplicationPoolName, ServerName);
						pool.InvokeMethod("Start", new object[0]);
					}

					if (Action == ApplicationPoolAction.Recycle)
					{
						Log.LogMessage(MessageImportance.Normal, "Recycling \"{0}\" on \"{1}\"...", ApplicationPoolName, ServerName);
						pool.InvokeMethod("Recycle", new object[0]);
					}

					bSuccess = true;
					Log.LogMessage(MessageImportance.Normal, "{0} \"{1}\" on \"{2}\"", GetActionFinish(), ApplicationPoolName, ServerName);
				}
				else
				{
					Log.LogError("Application Pools are only available in IIS 6.");
				}
			}
			catch (Exception ex)
			{
				Log.LogErrorFromException(ex);
				Log.LogError("Failed {0} application pool \"{1}\" on \"{1}\".", GetActionInProgress(), ApplicationPoolName, ServerName);
			}

			return bSuccess;
		}

		private string GetActionFinish()
		{
			switch (Action)
			{
				case ApplicationPoolAction.Restart:
					return "Restarted";
				case ApplicationPoolAction.Start:
					return "Started";
				case ApplicationPoolAction.Stop:
					return "Stopped";
				default:
					return "Recycled";
			}
		}

		private string GetActionInProgress()
		{
			switch (Action)
			{
				case ApplicationPoolAction.Restart:
					return "restarting";
				case ApplicationPoolAction.Start:
					return "starting";
				case ApplicationPoolAction.Stop:
					return "stopping";
				default:
					return "recycling";
			}
		}

		#endregion
	}
}
