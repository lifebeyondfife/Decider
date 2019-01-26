/*
  Copyright © Iain McDonald 2010-2019
  
  This file is part of Decider.
*/
using System;

namespace Decider.Csp.BaseTypes
{
	public class DeciderException : Exception
	{
		public DeciderException()
			: base()
		{
		}

		public DeciderException(string message)
			: base(message)
		{
		}
	}
}
