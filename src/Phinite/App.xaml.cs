using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;

namespace Phinite
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{

		public static readonly Version Version = Assembly.GetExecutingAssembly().GetName().Version;

		public static readonly String VersionString = Version.ToString();

		public static readonly String DefaultExample = "Yay!";

		/// <summary>
		/// Set of example regular expressions.
		/// </summary>
		public static readonly Dictionary<string, string> ExpressionExamples
			= new Dictionary<string, string>
			{
				{"Empty word", "."},
				{"Concatenation", "ababa"},
				{"Union", "aa+ab+ba+bb"},
				{"Empty word, concat. & union", "a(a+.)b(a+b)"},
				{"Kleene star", "ab^*"},
				{"Empty word, concat., union & star", "(a(a+.)(b(a+b))^*)^*"},
				{"Kleene plus", "a+b^+"},
				{"Parentheses", "(ab)^*+ab^*"},
				{"Binary numbers", "0+1(0+1)^*"},
				{"3 digit hexadecimal numbers", "(1+2+3+4+5+6+7)(0+1+2+3+4+5+6+7)(0+1+2+3+4+5+6+7)"},
				{"Example from old BA", "a^+c^+ + ab^+c"},
				{"Example from BA", "a(a+b)^*b"},
				{"Example from TA", "a^+b^+ + ab^+c"},
				{"High tree", "((((((((a^+b)^+c)^+d)^+e)^+f)^+g)^+i)^+j)^+k"},
				{"4 long paths", "aaaaaaae+bbbbbbe+ccccce+dddde"},
				{"Seemingly hard 1", "(a+ab+abc+abcd+abcde+abcdef)^*"},
				{"Seemingly hard 2", "(f+ef+def+cdef+bcdef+abcdef)^*"},
				{"Seemingly hard 3", "(a+.)^*b"},
				{"Seemingly hard 4", "(ab^*)^*"},
				{"Hard", "(((b)^*)((a((b)^*))^*))"},
				{"Pseudo e-mail", "(a+b+c+d+e+f+g+h+i+m+l+u+v+w+x+y+z)^+@(a+b+c+d+e+f+g+h+i+m+l+u+v+w+x+y+z)^+_(pl+eu+com+org+net)"},
				{"Yay!", "(A^+B^*C^+D^*E^+F^*G^+H^*I^+J^*K^+L^*M^+N^*O^+P^*R^+S^*T^+U^*V^+W^*X^+Y^*Z^+)^*"},
				{"Mess!", "(A^+B^*C^*D^*E^*F^*G^*H^*I^*J^*K^*L^*M^*N^*O^*P^*R^*S^+T^*U^*V^*W^*X^*Y^*Z^*)^*"},
				{"Infinite loop", "(a^*a)^*"},
				{"Infinite loop 2", "(a(a+.)b^*)^*"},
				{"Max processor use test", "0+(1+2+3+4+5+6+7+8+9)((0+1+2+3+4+5+6+7+8+9)^*(0+1+2+3+4+5+6+7+8+9))^*"},
				{"All features", "(.+bb)(aabb)^+(.+aa)+(aa+bb)^*(aa+.)"}
			};

		public static readonly Dictionary<string, string> WordExamples
			= new Dictionary<string, string>
			{
				{"Empty word", ""},
				{"Concatenation", "abab"},
				{"Union", "ba"},
				{"Empty word, concat. & union", "aabb"},
				{"Kleene star", "abbb"},
				{"Kleene plus", "bbbb"},
				{"Parentheses", "ababab"},
				{"Binary numbers", "11001010"},
				{"3 digit hexadecimal numbers", "103"},
				{"Example from old BA", "abbbc"},
				{"Example from BA", "abbaab"},
				{"Example from TA", "aaabbb"},
				{"High tree", "abcdefgijk"},
				{"4 long paths", "bbbbbbe"},
				{"Seemingly hard 1", "abcdeabc"},
				{"Seemingly hard 2", "defbcdef"},
				{"Seemingly hard 3", "aaab"},
				{"Seemingly hard 4", "abbabbb"},
				{"Hard", "bbbabbab"},
				{"Pseudo e-mail", "mb@mbdev_pl"},
				{"Yay!", "ABCDEFGHIJKLMNOPRSTUVWXYZ"},
				{"Mess!", "ABCDEFGHIJKLMNOPRSTUVWXYZ"},
				{"Infinite loop", "aaaa"},
				{"Infinite loop 2", "aabbabb"},
				{"Max processor use test", "5320481"},
				{"All features", "bbaabbaabbaabbaa"}
			};

	}
}
