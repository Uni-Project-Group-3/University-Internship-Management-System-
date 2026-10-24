using System;using System.Reflection;
class TestRunner { static int Main(string[] args){try{var assembly=Assembly.LoadFrom(args[0]);assembly.EntryPoint.Invoke(null,null);return 0;}catch(Exception error){Console.WriteLine("TEST FAILURE: "+(error.InnerException??error).Message);return 1;}} }
