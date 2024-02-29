using System;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;




namespace AntiLauncher.API.Threading {

	public static class TaskExtensions {

		public static ConfiguredTaskAwaitable ConfigureTask(this Task task)
			=> task.ConfigureAwait(false);

		public static ConfiguredTaskAwaitable<T> ConfigureTask<T>(this Task<T> task)
			=> task.ConfigureAwait(false);

	}

}
