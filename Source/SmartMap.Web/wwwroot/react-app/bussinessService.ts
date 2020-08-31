import { StateService } from './stateService';
import { http, HttpResponse } from './http';
import { ICardResponse, ICardCoordinate, ITag, ITranslation } from './service-model';

export interface IBussinessService {
    getCards(query: string, tags: string, transactionTags: string, region: string, languageCode: string, curretPage: number, digital: boolean, openNow: boolean, sorting: string, randomSeed: number): Promise<HttpResponse<ICardResponse>>;
    getCardCoordinates(query: string, tags: string, transactionTags: string, region: string, languageCode: string, digital: boolean, openNow: boolean): Promise<ICardCoordinate[]>;
    getTransactionTags(languageCode: string): Promise<ITag[]>;
    getTranslations(): Promise<ITranslation[]>;
}

export class BussinessService implements IBussinessService {
    private _baseUrl: string;
    private readonly GET: string = "GET";
    private readonly _partialUrl: string = "api/business";

    constructor(baseUrl: string) {
        this._baseUrl = baseUrl;
    }

    public async getTranslations(): Promise<ITranslation[]> {
        try {
            let languageCode = StateService.get<string>('common.languagecode');
            let response: HttpResponse<ITranslation[]> = await http<ITranslation[]>(
                `${this._baseUrl}/${this._partialUrl}/translation?lang=${languageCode}`,
                {
                    method: this.GET,
                    headers: this.getHeaders()
                }
            );

            if (response.data == null)
                return [];

            return response.data;
        }
        catch (exception) {
            this.handleError(exception);
        }
        return Promise.reject("Failed to get translations.");
    }

    public async getCards(
        query: string,
        tags: string,
        transactionTags: string,
        region: string,
        languageCode: string,
        curretPage: number,
        digital: boolean,
        openNow: boolean,
        sorting: string,
        randomSeed: number,
    ): Promise<HttpResponse<ICardResponse>> {
        let response: HttpResponse<ICardResponse>;
        try {
            const params = new URLSearchParams({
                region: region,
                page: curretPage?.toString(),
                lang: languageCode,
                query: query,
                tags: tags,
                transactionTags: transactionTags,
                digital: digital?.toString(),
                openNow: openNow?.toString(),
                sorting: sorting,
                randomSeed: randomSeed.toString(),
            });

            response = await http<ICardResponse>(
                `${this._baseUrl}/${this._partialUrl}?${params.toString()}`,
                {
                    method: this.GET,
                    headers: this.getHeaders()
                }
            );
            return response;
        }
        catch (exception) {
            this.handleError(exception);
        }
        return Promise.reject("Failed to get cards.");
    }

    public async getCardCoordinates(
        query: string,
        tags: string,
        transactionTags: string,
        region: string,
        languageCode: string,
        digital: boolean,
        openNow: boolean,
    ): Promise<ICardCoordinate[]> {
        let response: HttpResponse<ICardCoordinate[]>;
        try {
            const params = new URLSearchParams({
                region: region,
                lang: languageCode,
                query: query,
                tags: tags,
                transactionTags: transactionTags,
                digital: digital?.toString(),
                openNow: openNow?.toString(),
            });

            response = await http<ICardCoordinate[]>(
                `${this._baseUrl}/${this._partialUrl}/coordinates?${params.toString()}`,
                {
                    method: this.GET,
                    headers: this.getHeaders()
                }
            );

            if (response.data == null)
                return [];

            return response.data;
        }
        catch (exception) {
            this.handleError(exception);
        }
        return Promise.reject("Failed to get card coordinates.");
    }

    public async getTransactionTags(languageCode: string): Promise<ITag[]> {
        let response: HttpResponse<ITag[]>;
        try {
            response = await http<ITag[]>(
                `${this._baseUrl}/${this._partialUrl}/transactiontags?lang=${languageCode}`,
                {
                    method: this.GET,
                    headers: this.getHeaders()
                }
            );

            if (response.data == null)
                return [];

            return response.data;
        }
        catch (exception) {
            this.handleError(exception);
        }
        return Promise.reject("Failed to get transaciontags.");
    }

    private handleError(exception: any) {
        // TODO: sent to error api endpoint
        console.log("Error", exception);
    }

    private getHeaders(): Headers {
        let aft = document.getElementById('RequestVerificationToken') as HTMLInputElement;
        return new Headers({
            Accept: "application/json; charset=utf-8",
            RequestVerificationToken: aft.value,
        });
    }
}

export function getService() : IBussinessService | null {
    let baseUrl = StateService.get<string>('common.baseurl');

    let service: IBussinessService | null = null;
    if (baseUrl != null) {
        service = new BussinessService(baseUrl)
    }
    return service;
};