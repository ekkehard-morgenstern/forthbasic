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

\ === KEYBOARD HANDLERS =================================================================

\ --- EDITING KEYS ----------------------------------------------------------------------

: b-handle-page-up ( -- )
    ;

: b-handle-page-down ( -- )
    ;

: b-handle-home ( -- )
    ;

: b-handle-end ( -- )
    ;

: b-handle-insert ( -- )
    ;

: b-handle-delete ( -- )
    ;

\ --- FUNCTION KEYS ---------------------------------------------------------------------

: b-handle-f1 ( -- )
    ;

: b-handle-f2 ( -- )
    \ F2 changes the foreground color
    b-attribute @ ( attr )
    b-attr-fg-col+
    b-output-attr ;

: b-handle-f3 ( -- )
    \ F3 changes the background color
    b-attribute @ ( attr )
    b-attr-bg-col+
    b-output-attr ;

: b-handle-f4 ( -- )
    \ F4 changes the text mode
    b-attribute @ ( attr )
    b-attr-mode+
    b-output-attr ;

: b-handle-f5 ( -- )
    ;

: b-handle-f6 ( -- )
    ;

: b-handle-f7 ( -- )
    ;

: b-handle-f8 ( -- )
    ;

: b-handle-f9 ( -- )
    ;

: b-handle-f10 ( -- )
    ;

: b-handle-f11 ( -- )
    ;

: b-handle-f12 ( -- )
    \ set quit flag
    bc-true b-quit-flag ! ;

\ --- ESCAPE KEY ------------------------------------------------------------------------

: b-handle-escape ( -- )
    bye ;

\ --- CURSOR KEYS -----------------------------------------------------------------------

: b-handle-cursor-up ( -- )
    b-unmark-line
    \ get anticipated cursor position
    b-anticipate-cursor-up ( -- x y )
    \ locate to anticipated position
    b-locate b-mark-line ;

: b-handle-cursor-down ( -- )
    b-unmark-line
    \ get anticipated cursor position
    b-anticipate-cursor-down ( -- x y )
    \ locate to anticipated position
    b-locate b-mark-line ;

: b-handle-cursor-right ( -- )
    \ cursor right has been pressed
    \ get anticipated cursor position
    b-unmark-line 
    b-anticipate-next-char ( -- x y )
    \ locate to anticipated position
    b-locate b-mark-line ;

: b-handle-return ( -- )
    \ return key has been pressed
    b-unmark-line
    b-execute-line
    b-output-return 
    b-mark-line ;

: b-handle-cursor-left ( -- )
    \ cursor right has been pressed
    \ get anticipated cursor position
    b-unmark-line
    b-anticipate-prev-char ( -- x y )
    \ locate to anticipated position
    b-locate b-mark-line ;

: b-handle-backspace ( -- )
    \ backspace key has been pressed
    \ get anticipated cursor position
    b-unmark-line
    b-anticipate-prev-char ( -- x y )
    \ locate to anticipated position
    2dup b-locate 
    \ rub out character under cursor
    32 b-emit
    \ locate to anticipated position again
    b-locate b-mark-line ;

\ --- MAIN INPUT HANDLER ----------------------------------------------------------------

: b-input-handler ( -- )
    key? if
        ekey ekey>char if ( c ) 
            b-auto-update-window 
            case
                12      of b-handle-refresh endof
                13      of b-handle-return endof
                27      of b-handle-escape endof
                127     of b-handle-backspace endof
                ( c ) \ default handling:
                dup 32 < over 126 > or if ( c )
                else ( c )
                    b-unmark-line
                    dup b-emit
                    b-mark-line
                endif
            endcase

        else ekey>fkey if ( key-id )
            b-auto-update-window
            case
                \ cursor keys
                k-up    of b-handle-cursor-up       endof
                k-down  of b-handle-cursor-down     endof
                k-left  of b-handle-cursor-left     endof
                k-right of b-handle-cursor-right    endof
                \ pageup / pagedown
                k-prior of b-handle-page-up         endof
                k-next  of b-handle-page-down       endof
                \ home / end
                k-home  of b-handle-home            endof
                k-end   of b-handle-end             endof
                \ insert / delete
                k-insert of b-handle-insert         endof
                k-delete of b-handle-delete         endof
                \ function keys
                k-f1    of b-handle-f1              endof
                k-f2    of b-handle-f2              endof
                k-f3    of b-handle-f3              endof
                k-f4    of b-handle-f4              endof
                k-f5    of b-handle-f5              endof
                k-f6    of b-handle-f6              endof
                k-f7    of b-handle-f7              endof
                k-f8    of b-handle-f8              endof
                k-f9    of b-handle-f9              endof
                k-f10   of b-handle-f10             endof
                k-f11   of b-handle-f11             endof
                k-f12   of b-handle-f12             endof
            endcase
        else ( keyboard-event )
            drop

        then then
    then ;

\ --- SCREEN EDITOR MAIN ----------------------------------------------------------------

: b-screen-editor ( -- )
    \ BASIC screen editor
    begin 
        b-input-handler 
    b-quit-flag @ until 
    0 b-quit-flag ! ;

: basic ( -- )
    \ return to BASIC from FORTH
    b-auto-update-window b-handle-refresh 
    b-cursor-x @ b-cursor-y @ b-locate
    b-mark-line 
    b-screen-editor ;


