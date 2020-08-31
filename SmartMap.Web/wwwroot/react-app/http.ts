// url: https://www.carlrippon.com/fetch-with-async-await-and-typescript/
// url: https://developer.mozilla.org/en-US/docs/Web/API/Fetch_API/Using_Fetch
export interface HttpResponse<T> extends Response {
    data?: T;
}

export async function http<T>(request: RequestInfo, init?: RequestInit): Promise<HttpResponse<T>> {
    const response: HttpResponse<T> = await fetch(
        request, 
        init
    );

    try {
        // may error if there is no body
        response.data = await response.json();
    }
    catch (ex) {
        throw new Error('Failed convertion to json object. exception:' + ex);
    }

    if (!response.ok) {
        throw new Error(response.statusText);
    }
    return response;
}