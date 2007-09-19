using System;
using ThoughtWorks.CruiseControl.Core.Util;
using ThoughtWorks.CruiseControl.Remote;

namespace ThoughtWorks.CruiseControl.Core
{
	public class IntegrationResultManager : IIntegrationResultManager
	{
		private readonly Project project;
		private IIntegrationResult lastResult;
		private IIntegrationResult currentIntegration;
		private IntegrationSummary lastIntegration;

		public IntegrationResultManager(Project project)
		{
			this.project = project;
		}

		public IIntegrationResult LastIntegrationResult
		{
			get
			{
				// lazy loads because StateManager needs to be populated from configuration
				if (lastResult == null)
				{
					lastResult = CurrentIntegration;
				}
				return lastResult;
			}
		}

		public IntegrationSummary LastIntegration
		{
			get
			{
				if (lastIntegration == null)
				{
					lastIntegration = ConvertResultIntoSummary(LastIntegrationResult);
				}
				return lastIntegration;
			}
		}

		public IIntegrationResult CurrentIntegration
		{
			get
			{
				if (currentIntegration == null)
				{
					if (project.StateManager.HasPreviousState(project.Name))
						currentIntegration = project.StateManager.LoadState(project.Name);
					else
						currentIntegration = IntegrationResult.CreateInitialIntegrationResult(project.Name, project.WorkingDirectory, project.ArtifactDirectory);
				}
				return currentIntegration;
			}
		}

		public IIntegrationResult StartNewIntegration(IntegrationRequest request)
		{
			IntegrationResult newResult = new IntegrationResult(project.Name, project.WorkingDirectory, project.ArtifactDirectory, request, LastIntegration);
			newResult.ArtifactDirectory = project.ArtifactDirectory;
			newResult.ProjectUrl = project.WebURL;
			return currentIntegration = newResult;
		}

		public void FinishIntegration()
		{
			try
			{
				project.StateManager.SaveState(currentIntegration);
			}
			catch (Exception ex)
			{
				// swallow exception???
				Log.Error("Unable to save integration result: " + ex);
			}
			lastResult = currentIntegration;
			lastIntegration = ConvertResultIntoSummary(currentIntegration);
		}

		private static IntegrationSummary ConvertResultIntoSummary(IIntegrationResult integration)
		{
			string lastSuccessfulIntegrationLabel = (integration.Succeeded) ? integration.Label : integration.LastSuccessfulIntegrationLabel;
			return new IntegrationSummary(integration.Status, integration.Label, lastSuccessfulIntegrationLabel, integration.StartTime);
		}
	}
}