// Copyright (c) 2004, Rodrigo B. de Oliveira (rbo@acm.org)
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without modification,
// are permitted provided that the following conditions are met:
// 
//     * Redistributions of source code must retain the above copyright notice,
//     this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright notice,
//     this list of conditions and the following disclaimer in the documentation
//     and/or other materials provided with the distribution.
//     * Neither the name of the Rodrigo B. de Oliveira nor the names of its
//     contributors may be used to endorse or promote products derived from this
//     software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
// ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE
// FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
// SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
// CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
// OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF
// THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

options
{
	language="CSharp";
	namespace = "Boo.AntlrParser";
}
class BooExpressionLexer extends Lexer;
options
{
	defaultErrorHandler = false;
	testLiterals = false;
	importVocab = Boo;	
	k = 2;
	charVocabulary='\u0003'..'\uFFFE';
	// without inlining some bitset tests, ANTLR couldn't do unicode;
	// They need to make ANTLR generate smaller bitsets;
	codeGenBitsetTestThreshold=20;	
	classHeaderPrefix="internal";
}
{
	
	public override void uponEOF()
	{
		Error();
	}

	void Error()
	{		
		throw new SemanticException("Unterminated expression interpolation!", getFilename(), getLine(), getColumn());
	}
}
ID options { testLiterals = true; }:
	ID_LETTER (ID_LETTER | DIGIT)*
	;

INT : 
	("0x"(HEXDIGIT)+)(('l' | 'L') { $setType(LONG); })? |
	(DIGIT)+
	(
		('l' | 'L') { $setType(LONG); } |
		(
			({BooLexer.IsDigit(LA(2))}? ('.' (DIGIT)+) { $setType(DOUBLE); })?
			(("ms" | 's' | 'm' | 'h' | 'd') { $setType(TIMESPAN); })?
		)
	)
	;

DOT : '.' ((DIGIT)+ {$setType(DOUBLE);})?;

COLON : ':';

COMMA : ',';

BITWISE_OR: '|';

BITWISE_AND: '&';

LPAREN : '(';
	
RPAREN : ')';

LBRACK : '[';

RBRACK : ']';

LBRACE : '{';
	
RBRACE : '}';

INCREMENT: "++";

DECREMENT: "--";

ADD: ('+') ('=' { $setType(ASSIGN); })?;

SUBTRACT: ('-') ('=' { $setType(ASSIGN); })?;

MODULUS: '%';

MULTIPLY: '*' (
					'=' { $setType(ASSIGN); } |
					'*' { $setType(EXPONENTIATION); } | 
				);

DIVISION: 
	(RE_LITERAL)=> RE_LITERAL { $setType(RE_LITERAL); } |
	'/' ('=' { $setType(ASSIGN); })?
	;


CMP_OPERATOR : '<' | "<=" | '>' | ">=" | "!~" | "!=";

ASSIGN : '=' ( ('=' | '~') { $setType(CMP_OPERATOR); } )?;

WS: (' ' | '\t' { tab(); } | '\r' | '\n' { newline(); })+ { $setType(Token.SKIP); };

SINGLE_QUOTED_STRING :
		'\''!
		(
			SQS_ESC |
			~('\'' | '\\' | '\r' | '\n')
		)*
		'\''!
		;

protected
DQS_ESC : '\\'! ( SESC | '"' | '$') ;	
	
protected
SQS_ESC : '\\'! ( SESC | '\'' );

protected
SESC : 
				( 'r' {$setText("\r"); }) |
				( 'n' {$setText("\n"); }) |
				( 't' {$setText("\t"); }) |
				( '\\' {$setText("\\"); });

protected
RE_LITERAL : '/' (RE_CHAR)+ '/';

protected
RE_CHAR : RE_ESC | ~('/' | '\\' | '\r' | '\n' | ' ' | '\t');

protected
RE_ESC : '\\' (
	
	// character scapes
	// ms-help://MS.NETFrameworkSDKv1.1/cpgenref/html/cpconcharacterescapes.htm
	
				'a' |
				'b' |
				'c' 'A'..'Z' |
				't' |
				'r' |
				'v' |
				'f' |
				'n' |
				'e' |
				(DIGIT)+ |
				'x' DIGIT DIGIT |
				'u' DIGIT DIGIT DIGIT DIGIT |
				'\\' |
				
	// character classes
	// ms-help://MS.NETFrameworkSDKv1.1/cpgenref/html/cpconcharacterclasses.htm
	// /\w\W\s\S\d\D/
	
				'w' |
				'W' |
				's' |
				'S' |
				'd' |
				'D' |
				'p' |
				'P' |
				
	// atomic zero-width assertions
	// ms-help://MS.NETFrameworkSDKv1.1/cpgenref/html/cpconatomiczero-widthassertions.htm
				'A' |
				'z' |
				'Z' |
				'g' |
				'B' |
				
				'k' |
				
				'/' |
				'(' |
				')' |
				'|' |
				'.' |
				'*' |
				'?' |
				'$' |
				'^' |
				'['	|
				']'
			 )
			 ;

protected
ID_LETTER : ('_' | 'a'..'z' | 'A'..'Z' );

protected
DIGIT : '0'..'9';

protected
HEXDIGIT : ('a'..'f' | 'A'..'F' | '0'..'9');
