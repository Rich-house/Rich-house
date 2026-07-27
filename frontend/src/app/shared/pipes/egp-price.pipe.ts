import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'egpPrice',
})
export class EgpPricePipe implements PipeTransform {
  private readonly formatter = new Intl.NumberFormat('en-EG', {
    maximumFractionDigits: 0,
  });

  transform(value: number | null | undefined): string {
    if (typeof value !== 'number' || Number.isNaN(value)) {
      return 'EGP —';
    }

    return `EGP ${this.formatter.format(value)}`;
  }
}
