using System;

namespace RedisCacheLoad
{
	/// <summary>
	/// Cached version of the SdkUser because the TableEntity derived version is not serializable
	/// </summary>
	[Serializable]
	public class SdkUser
	{
		public string Name { get; set; }
		public bool Enabled { get; set; }
		public int Id { get; set; }
		public string SecurityKey { get; set; }
		public DateTime Expires { get; set; }

		public SdkUser() { }
	}
}
