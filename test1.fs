#! /usr/bin/gforth

3 5 + .

." Hello world!"

: squared ( n -- n^2 )
    dup *
;

2 squared .

: log2 ( +n1 -- n2 )
\ logarithmus dualis of n1>0, rounded down to the next integer
  assert( dup 0 > )
  -1 begin
    1+ swap 2/ swap
    over 0 <=
  until
  nip ;

4 log2 .
8 log2 .

: ^ ( n1 u -- n )
\ n = the uth power of n1
  1 swap 0 u+do
    over *
  loop
  nip ;

2 3 ^ .
4 4 ^ .

bye

