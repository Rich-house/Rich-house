import { Directive, ElementRef, HostListener, OnChanges, SimpleChanges } from '@angular/core';

@Directive({
  selector: '[appBoxshadow]',
})
export class Boxshadow {

  constructor(private ele: ElementRef) {}

  @HostListener('mouseenter')
  addShadow() {
    this.ele.nativeElement.style.boxShadow = '0 0 15px red';
  }

  @HostListener('mouseleave')
  removeShadow() {
    this.ele.nativeElement.style.boxShadow = '';
  }
}

  
