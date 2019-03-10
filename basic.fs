
: ESC ( -- )
    \ output escape character
    27 emit ;

: CSI ( -- )
    \ output control sequence introducer ( ESC [ )
    ESC ." [" ;

: SEMIC ( -- ) 
    \ output semicolon
    ." ;" ;

: outdig ( n -- )
    \ output least significant digit of decimal number
    10 mod 48 + emit ;

: outnum ( n -- )
    \ output decimal number (without surrounding blanks)
    dup 0= if 
        \ if the value is zero, only output that
        drop ." 0"
    else
        \ if the number is negative, output minus sign and negate
        dup 0< if
            ." -"
            negate
        endif
        \ divide number by 10
        dup 10 /
        \ if non-zero, recurse with that number
        dup 0<> if
            recurse
        else
            drop
        endif
        outdig
    endif ;

: locate ( x y -- )
    \ set cursor to specified screen position
    CSI outnum SEMIC outnum ." H" ;

: cls ( -- )
    \ clear screen and set cursor to top left screen position
    CSI ." 2J" 
    1 1 locate ;


