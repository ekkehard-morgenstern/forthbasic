
27 constant bc-escape
48 constant bc-digit-lo
57 constant bc-digit-hi
0 constant bc-false
-1 constant bc-true
256 constant bc-max-line-size
128 constant bc-max-lines

create b-linebuf bc-max-line-size chars allot
create b-screenbuf bc-max-lines bc-max-line-size * chars allot

: b-ESC ( -- )
    \ output escape character
    bc-escape emit ;

: b-CSI ( -- )
    \ output control sequence introducer ( ESC [ )
    b-ESC ." [" ;

: b-SEMIC ( -- ) 
    \ output semicolon
    ." ;" ;

: b-outnum ( n -- )
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
        \ output least significant digit of decimal number
        10 mod bc-digit-lo + emit
    endif ;

: b-locate ( x y -- )
    \ set cursor to specified screen position (starting from 1,1)
    \ at-xy starts from (0,0)
    1 - swap 1 - swap at-xy ;

: b-cls ( -- )
    \ clear screen and set cursor to top left screen position
    page ;

: b-kc-digit? ( k -- t )
    \ analyzes a key code to see if it's a digit
    dup bc-digit-lo < if
        drop
        bc-false
    else
        bc-digit-hi > if
        bc-false
        else
        bc-true
        endif
    endif ;

: b-kc-escape? ( k -- t )
    \ check if a key code is the escape key
    bc-escape = if
        bc-true
    else
        bc-false
    endif ;

: b-handle-escape-sequence ( k -- )
    \ if the keycode is the escape key, check for the control sequence introducer.
    \ if it's the CSI, read keys until a non-digit non-semicolon is encountered, 
    \ pushing numbers onto the stack.
    ;

: b-line-editor ( -- )
    \ BASIC line editor
    ;

: b-screen-editor ( -- )
    \ BASIC screen editor
    ;

