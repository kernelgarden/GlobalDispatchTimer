using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Global.Timer.Utils;
using UnityEngine;

namespace Global.Timer
{
	public class GroupTask : TimerJob
	{
		private List<Action> scheduledTasks;
		
		public GroupTask(IEnumerable<Action> newTasks, float executedTicks) : base(null, executedTicks)
		{
			scheduledTasks = newTasks.ToList();
		}

		public void ExecuteInternal()
		{
			foreach (var task in scheduledTasks)
				task.Execute();
		}
		
		public void AddTask(Action newTask)
		{
			scheduledTasks.Add(newTask);
		}

		public void RemoveTask(Action targetTask)
		{
			scheduledTasks.Remove(targetTask);
		}

		public int IndexOf(Action targetTask)
		{
			return scheduledTasks.FindIndex(task => task.Equals(targetTask));
		}

		public override void Execute()
		{
			if (!isDeActivated)
				ExecuteInternal();
		}
	}
}
