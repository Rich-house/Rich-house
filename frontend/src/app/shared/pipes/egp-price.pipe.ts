import { Pipe, PipeTransform } from '@angular/core';
import { formatEgpPrice } from '../../core/utils/price';

@Pipe({
  name: 'egpPrice',
})
export class EgpPricePipe implements PipeTransform {
  transform(value: number | null | undefined): string {
    return formatEgpPrice(value);
  }
}
