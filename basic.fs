
13 constant bc-cr
10 constant bc-lf
27 constant bc-escape
48 constant bc-digit-lo
57 constant bc-digit-hi
0 constant bc-false
-1 constant bc-true
256 constant bc-max-line-size
128 constant bc-max-lines

variable b-window-width
variable b-window-height
variable b-window-size

: b-update-window-size ( -- )
    form b-window-width ! b-window-height ! 
    b-window-width @ b-window-height @ * b-window-size ! ;

b-update-window-size

: b-NEWLINE
    \ output newline character(s)
    bc-cr emit bc-lf emit ;

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
    \ set cursor to specified screen position (starting from 0,0)
    at-xy ;

: b-cls ( -- )
    \ clear screen and set cursor to top left screen position
    page ;

b-cls

." Forth BASIC v0.1 - Copyright (c) Ekkehard Morgenstern. All rights reserved."
b-NEWLINE
." Licensable under the GNU General Public License (GPL) v3 or higher."
b-NEWLINE
." Written for use with GNU Forth (aka GForth)."
b-NEWLINE
b-NEWLINE

: b-input-handler ( -- )
    key? if
        ekey ekey>char if ( c ) 

        else ekey>fkey if ( key-id )
            case
                \ cursor keys
                k-up    of   endof
                k-down  of   endof
                k-left  of   endof
                k-right of   endof
                \ pageup / pagedown
                k-prior of   endof
                k-next  of   endof
                \ home / end
                k-home  of   endof
                k-end   of   endof
                \ insert / delete
                k-insert of  endof
                k-delete of  endof
                \ function keys
                k-f1    of   endof
                k-f2    of   endof
                k-f3    of   endof
                k-f4    of   endof
                k-f5    of   endof
                k-f6    of   endof
                k-f7    of   endof
                k-f8    of   endof
                k-f9    of   endof
                k-f10   of   endof
                k-f11   of   endof
                k-f12   of   endof
            endcase
        else ( keyboard-event )
            drop

        then then
    then ;

: b-screen-editor ( -- )
    \ BASIC screen editor
    ;

