\ Forth BASIC v0.1 - A BASIC interpreter written in FORTH.
\ Copyright (C) 2019 Ekkehard Morgenstern
\ 
\ This program is free software: you can redistribute it and/or modify
\ it under the terms of the GNU General Public License as published by
\ the Free Software Foundation, either version 3 of the License, or
\ (at your option) any later version.
\ 
\ This program is distributed in the hope that it will be useful,
\ but WITHOUT ANY WARRANTY; without even the implied warranty of
\ MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
\ GNU General Public License for more details.
\ 
\ You should have received a copy of the GNU General Public License
\ along with this program.  If not, see <https://www.gnu.org/licenses/>.
\ 
\ Programs written in Forth BASIC do not fall under this license except 
\ where otherwise noted.
\ 
\ This version has been written for use with GNU Forth (aka GForth). 
\ Contact info: ekkehard@ekkehardmorgenstern.de

\ === CONTROL SEQUENCES =================================================================

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

: b-set-autowrap ( -- )
    b-CSI ." ?7h" ;

: b-reset-autowrap ( -- )
    b-CSI ." ?7l" ;

: b-clear-buffer ( -- )
    \ clear text buffer
    b-window-buffer @ b-window-size @ ( addr count )
    0 +do ( addr )
        0 over !
        cell+
    loop drop ;

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

: b-ansi-color ( attr -- )
    \ output ANSI color sequence for specified attributes
    b-split-attr rot    ( bgcol fgcol mode )
    dup case ( mode )
        0 of  
            \ mode 0 deactivates attributes (normal mode): keep
        endof
        1 of   
            \ mode 1 puts it in bold face / high intesity : keep
        endof
        2 of   
            \ mode 2 makes it blink : in ANSI, 5
            drop 5
        endof
        3 of   
            \ mode 3 reverse video : in ANSI, 7
            drop 7
        endof
    endcase
    b-CSI 
    ( bgcol fgcol mode )
    b-outnum b-SEMIC        \ output new mode
    30 + b-outnum b-SEMIC   \ set foreground color
    40 + b-outnum           \ set background color
    ." m" ;

: b-ansi-undo ( attr -- )
    \ output ANSI undo sequence for specified attribute
    b-split-attr rot    ( bgcol fgcol mode )
    dup case ( mode )
        0 of  
            \ mode 0 deactivates attributes (normal mode): keep
        endof
        1 of   
            \ mode 1 puts it in bold face / high intesity : keep
            drop 22     \ undo bold/faint
        endof
        2 of   
            \ mode 2 makes it blink : in ANSI, 5
            drop 25     \ undo blink
        endof
        3 of   
            \ mode 3 reverse video : in ANSI, 7
            drop 27     \ undo reverse
        endof
    endcase
    b-CSI 
    b-outnum b-SEMIC    \ output new mode
    ( bgcol fgcol )     \ ignore color
    2drop
    ." m" ;

: b-clear ( -- )
    \ clear screen and set cursor to top left screen position (raw)
    b-ESC ." c" 
    \ after that, emit current attribute
    b-attribute @ b-ansi-color 
    \ clear mark
    0 b-mark-flag ! ;

: b-clear-def ( -- )
    \ clear screen and set cursor to top left screen position (raw)
    b-ESC ." c" 
    \ after that, emit default attribute
    b-default-attribute @ b-ansi-color ;

: b-output-attr ( attr -- )
    \ output attribute
    \ mask off unwanted bits
    0xff00 and ( attr )
    \ see if attribute is zero
    dup 0= if
        \ yes: use default attribute
        drop b-default-attribute @
    endif
    \ see if attribute has changed
    dup b-attribute @ <> if     \ if attribute has changed
        \ yes: ( attr )
        dup bc-mode-mask and    \ mask off non-mode bits in new attribute
        b-attribute @           \ get previous attribute
        bc-mode-mask and        \ mask off non-mode bits 
        <> if                   \ if mode has changed
            b-attribute @
            b-ansi-undo         \ undo old mode
        then
        dup b-attribute !       \ remember new attribute
        b-ansi-color            \ output that
    else
        \ no: ignore
        drop
    endif ;

: b-cell-emit ( chr -- )
    \ low-level cell emitter
    dup b-output-attr \ output attribute
    255 and     \ remove attribute bits
    dup 0= if   \ if zero, convert to space ' '
        drop 32
    then
    emit ;

